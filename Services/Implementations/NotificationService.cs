using AutoMapper;
using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using Services.Contracts;
using Services.Models.Notification;
using Utils;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;

namespace Services.Implementations
{
    public class NotificationService : INotificationService
    {
        private readonly IDatabaseFactory _databaseFactory;
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<NotificationService> _logger;
        private readonly INotificationHubService? _hubService;

        public NotificationService(
            IDatabaseFactory databaseFactory,
            IUserAccountRepository userAccountRepository,
            IMapper mapper,
            ILogger<NotificationService> logger,
            INotificationHubService? hubService = null)
        {
            _databaseFactory = databaseFactory;
            _userAccountRepository = userAccountRepository;
            _mapper = mapper;
            _logger = logger;
            _hubService = hubService;
        }

        private IMongoRepository<Notification> GetRepository()
        {
            return _databaseFactory.GetRepositoryByType<IMongoRepository<Notification>>(DatabaseType.MongoDb);
        }

        private Dictionary<string, object> ConvertMetadataForMongoDB(Dictionary<string, object> metadata)
        {
            var convertedMetadata = new Dictionary<string, object>();
            
            foreach (var kvp in metadata)
            {
                var value = kvp.Value;
                
                if (value is System.Text.Json.JsonElement jsonElement)
                {
                    convertedMetadata[kvp.Key] = ConvertJsonElement(jsonElement);
                }
                else
                {
                    convertedMetadata[kvp.Key] = value;
                }
            }
            
            return convertedMetadata;
        }

        private object ConvertJsonElement(System.Text.Json.JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                System.Text.Json.JsonValueKind.String => jsonElement.GetString()!,
                System.Text.Json.JsonValueKind.Number => jsonElement.TryGetInt32(out var intValue) ? intValue : jsonElement.GetDouble(),
                System.Text.Json.JsonValueKind.True => true,
                System.Text.Json.JsonValueKind.False => false,
                System.Text.Json.JsonValueKind.Null => null!,
                System.Text.Json.JsonValueKind.Array => jsonElement.EnumerateArray().Select(ConvertJsonElement).ToArray(),
                System.Text.Json.JsonValueKind.Object => jsonElement.EnumerateObject().ToDictionary(
                    prop => prop.Name, 
                    prop => ConvertJsonElement(prop.Value)
                ),
                _ => jsonElement.ToString()
            };
        }

        public async Task<NotificationResponse> CreateNotificationAsync(CreateNotificationDto dto)
        {
            try
            {
                var notification = _mapper.Map<Notification>(dto);
                notification.Id = Guid.NewGuid();
                notification.TimeStamp = DateTime.UtcNow;
                notification.Status = NotificationStatus.Unread;

                if (notification.Metadata != null)
                {
                    notification.Metadata = ConvertMetadataForMongoDB(notification.Metadata);
                }

                var repository = GetRepository();
                var created = await repository.AddAsync(notification);
                var response = _mapper.Map<NotificationResponse>(created);
                
                // Send real-time notification
                try
                {
                    if (_hubService != null)
                    {
                        await _hubService.SendNotificationToUserAsync(dto.UserId, response);
                        var unreadCount = await GetUnreadCountAsync(dto.UserId);
                        await _hubService.SendNotificationCountUpdateAsync(dto.UserId, unreadCount);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending real-time notification for user {UserId}", dto.UserId);
                    // Don't fail the notification creation if real-time fails
                }
                
                _logger.LogInformation("Created notification {NotificationId} for user {UserId}", created.Id, dto.UserId);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification for user {UserId}", dto.UserId);
                throw;
            }
        }

        public async Task<NotificationResponse> GetNotificationByIdAsync(string id)
        {
            if (!Guid.TryParse(id, out var guidId))
                throw new ArgumentException("Invalid notification ID format");
                
            var repository = GetRepository();
            var notification = await repository.FindAsync(guidId);
            
            if (notification == null)
                throw new InvalidOperationException($"Notification with ID {id} not found");
                
            return _mapper.Map<NotificationResponse>(notification);
        }

        public async Task<IEnumerable<NotificationResponse>> GetNotificationsByUserIdAsync(Guid userId, int page = 1, int pageSize = 20)
        {
            var repository = GetRepository();
            var filter = Builders<Notification>.Filter.Eq(n => n.UserId, userId);
            var sort = Builders<Notification>.Sort.Descending(n => n.TimeStamp);
            
            var notifications = await repository.FindByFilterAsync(filter, sort, (page - 1) * pageSize, pageSize);
            return _mapper.Map<IEnumerable<NotificationResponse>>(notifications);
        }

        public async Task<NotificationResponse> MarkAsReadAsync(string notificationId, Guid userId)
        {
            if (!Guid.TryParse(notificationId, out var guidId))
                throw new ArgumentException("Invalid notification ID format");
                
            var repository = GetRepository();
            var notification = await repository.FindAsync(guidId);
            
            if (notification == null || notification.UserId != userId)
                throw new InvalidOperationException("Notification not found or access denied");
                
            notification.Status = NotificationStatus.Read;
            notification.ReadAt = DateTime.UtcNow;
            
            var updated = await repository.UpdateAsync(notification);
            return _mapper.Map<NotificationResponse>(updated);
        }

        public async Task<NotificationResponse> AcknowledgeNotificationAsync(string notificationId, Guid userId)
        {
            if (!Guid.TryParse(notificationId, out var guidId))
                throw new ArgumentException("Invalid notification ID format");
                
            var repository = GetRepository();
            var notification = await repository.FindAsync(guidId);
            
            if (notification == null || notification.UserId != userId)
                throw new InvalidOperationException("Notification not found or access denied");
                
            notification.Status = NotificationStatus.Acknowledged;
            notification.AcknowledgedAt = DateTime.UtcNow;
            
            var updated = await repository.UpdateAsync(notification);
            return _mapper.Map<NotificationResponse>(updated);
        }

        public async Task<bool> DeleteNotificationAsync(string notificationId, Guid userId)
        {
            if (!Guid.TryParse(notificationId, out var guidId))
                return false;
                
            var repository = GetRepository();
            var notification = await repository.FindAsync(guidId);
            
            if (notification == null || notification.UserId != userId)
                return false;
                
            await repository.DeleteAsync(guidId);
            return true;
        }

        public async Task<IEnumerable<NotificationResponse>> MarkAllAsReadAsync(Guid userId)
        {
            var repository = GetRepository();
            var filter = Builders<Notification>.Filter.And(
                Builders<Notification>.Filter.Eq(n => n.UserId, userId),
                Builders<Notification>.Filter.Eq(n => n.Status, NotificationStatus.Unread)
            );
            
            var update = Builders<Notification>.Update
                .Set(n => n.Status, NotificationStatus.Read)
                .Set(n => n.ReadAt, DateTime.UtcNow);
                
            // Note: This would need to be implemented in the repository for bulk updates
            // For now, we'll get all unread notifications and update them individually
            var unreadNotifications = await repository.FindByFilterAsync(filter);
            var updatedNotifications = new List<NotificationResponse>();
            
            foreach (var notification in unreadNotifications)
            {
                notification.Status = NotificationStatus.Read;
                notification.ReadAt = DateTime.UtcNow;
                var updated = await repository.UpdateAsync(notification);
                updatedNotifications.Add(_mapper.Map<NotificationResponse>(updated));
            }
            
            return updatedNotifications;
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            var repository = GetRepository();
            var filter = Builders<Notification>.Filter.And(
                Builders<Notification>.Filter.Eq(n => n.UserId, userId),
                Builders<Notification>.Filter.Eq(n => n.Status, NotificationStatus.Unread)
            );
            
            var notifications = await repository.FindByFilterAsync(filter);
            return notifications.Count();
        }

        public async Task<IEnumerable<NotificationResponse>> GetNotificationsByTypeAsync(NotificationType type, int page = 1, int pageSize = 20)
        {
            var repository = GetRepository();
            var filter = Builders<Notification>.Filter.Eq(n => n.NotificationType, type);
            var sort = Builders<Notification>.Sort.Descending(n => n.TimeStamp);
            
            var notifications = await repository.FindByFilterAsync(filter, sort, (page - 1) * pageSize, pageSize);
            return _mapper.Map<IEnumerable<NotificationResponse>>(notifications);
        }

        public async Task<NotificationResponse> CreateAdminNotificationAsync(CreateAdminNotificationDto dto)
        {
            try
            {
                // Get all admin users from the database
                var adminUsers = await _userAccountRepository.GetAdminUsersAsync();
                
                if (!adminUsers.Any())
                {
                    _logger.LogWarning("No admin users found in the system");
                    throw new InvalidOperationException("No admin users available to receive notifications");
                }
                
                // For now, send to the first admin user (in a real system, you might want to send to all admins)
                var primaryAdmin = adminUsers.First();
                
                var createDto = new CreateNotificationDto
                {
                    UserId = primaryAdmin.Id,
                    Title = dto.Title,
                    Message = dto.Message,
                    NotificationType = dto.NotificationType,
                    RecipientType = RecipientType.Admin,
                    Priority = dto.Priority,
                    RelatedEntityId = dto.RelatedEntityId,
                    RelatedEntityType = dto.RelatedEntityType,
                    ActionRequired = dto.ActionRequired,
                    ActionUrl = dto.ActionUrl,
                    ExpiresAt = dto.ExpiresAt,
                    Metadata = dto.Metadata
                };
                
                _logger.LogInformation("Creating admin notification for admin {AdminId} ({AdminName})", 
                    primaryAdmin.Id, $"{primaryAdmin.FirstName} {primaryAdmin.LastName}");
                
                var response = await CreateNotificationAsync(createDto);
                
                // Send real-time notification to admins
                try
                {
                    if (_hubService != null)
                    {
                        await _hubService.SendNotificationToAdminsAsync(response);
                        var adminUnreadCount = await GetAdminUnreadCountAsync();
                        await _hubService.SendNotificationCountUpdateToAdminsAsync(adminUnreadCount);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending real-time admin notification");
                }
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating admin notification");
                throw;
            }
        }

        public async Task<IEnumerable<NotificationResponse>> GetAdminNotificationsAsync(int page = 1, int pageSize = 20)
        {
            var repository = GetRepository();
            var filter = Builders<Notification>.Filter.Eq(n => n.RecipientType, RecipientType.Admin);
            var sort = Builders<Notification>.Sort.Descending(n => n.TimeStamp);
            
            var notifications = await repository.FindByFilterAsync(filter, sort, (page - 1) * pageSize, pageSize);
            return _mapper.Map<IEnumerable<NotificationResponse>>(notifications);
        }

        public async Task<IEnumerable<NotificationResponse>> GetNotificationsRequiringActionAsync()
        {
            var repository = GetRepository();
            var filter = Builders<Notification>.Filter.And(
                Builders<Notification>.Filter.Eq(n => n.ActionRequired, true),
                Builders<Notification>.Filter.Ne(n => n.Status, NotificationStatus.Acknowledged)
            );
            var sort = Builders<Notification>.Sort.Descending(n => n.Priority).Descending(n => n.TimeStamp);
            
            var notifications = await repository.FindByFilterAsync(filter, sort);
            return _mapper.Map<IEnumerable<NotificationResponse>>(notifications);
        }

        public async Task<NotificationResponse> CreateSystemNotificationAsync(CreateSystemNotificationDto dto)
        {
            var createDto = new CreateNotificationDto
            {
                UserId = Guid.Empty, // System notifications might not have a specific user
                Title = dto.Title,
                Message = dto.Message,
                NotificationType = dto.NotificationType,
                RecipientType = dto.RecipientType,
                Priority = dto.Priority,
                RelatedEntityId = dto.RelatedEntityId,
                RelatedEntityType = dto.RelatedEntityType,
                ActionRequired = dto.ActionRequired,
                ActionUrl = dto.ActionUrl,
                ExpiresAt = dto.ExpiresAt,
                Metadata = dto.Metadata
            };
            
            return await CreateNotificationAsync(createDto);
        }

        public async Task CleanupExpiredNotificationsAsync()
        {
            var repository = GetRepository();
            var filter = Builders<Notification>.Filter.And(
                Builders<Notification>.Filter.Lt(n => n.ExpiresAt, DateTime.UtcNow),
                Builders<Notification>.Filter.Ne(n => n.Status, NotificationStatus.Expired)
            );
            
            var expiredNotifications = await repository.FindByFilterAsync(filter);
            
            foreach (var notification in expiredNotifications)
            {
                notification.Status = NotificationStatus.Expired;
                await repository.UpdateAsync(notification);
            }
            
            _logger.LogInformation("Cleaned up {Count} expired notifications", expiredNotifications.Count());
        }

        // Driver leave specific notification methods
        public async Task<NotificationResponse> CreateDriverLeaveNotificationAsync(Guid leaveRequestId, NotificationType type, string message, Dictionary<string, object>? metadata = null)
        {
            var createDto = new CreateAdminNotificationDto
            {
                Title = GetNotificationTitle(type),
                Message = message,
                NotificationType = type,
                Priority = GetNotificationPriority(type),
                RelatedEntityId = leaveRequestId,
                RelatedEntityType = "DriverLeaveRequest",
                ActionRequired = true,
                ActionUrl = $"/admin/leave-requests/{leaveRequestId}",
                ExpiresAt = GetNotificationExpiration(type),
                Metadata = metadata
            };
            
            return await CreateAdminNotificationAsync(createDto);
        }

        public async Task<NotificationResponse> CreateReplacementSuggestionNotificationAsync(Guid leaveRequestId, int suggestionCount, Dictionary<string, object>? metadata = null)
        {
            var message = $"Auto-generated {suggestionCount} replacement suggestion(s) for leave request. Please review and approve.";
            return await CreateDriverLeaveNotificationAsync(leaveRequestId, NotificationType.ReplacementSuggestion, message, metadata);
        }

        private string GetNotificationTitle(NotificationType type)
        {
            return type switch
            {
                NotificationType.DriverLeaveRequest => "New Leave Request",
                NotificationType.ReplacementSuggestion => "Replacement Suggestions Available",
                NotificationType.LeaveApproval => "Leave Request Updated",
                NotificationType.ConflictDetected => "Schedule Conflict Detected",
                NotificationType.ReplacementAccepted => "Replacement Accepted",
                NotificationType.ReplacementRejected => "Replacement Rejected",
                NotificationType.SystemAlert => "System Alert",
                NotificationType.MaintenanceReminder => "Maintenance Reminder",
                NotificationType.ScheduleChange => "Schedule Change",
                NotificationType.EmergencyNotification => "Emergency Notification",
                _ => "System Notification"
            };
        }

        private int GetNotificationPriority(NotificationType type)
        {
            return type switch
            {
                NotificationType.EmergencyNotification => 4,
                NotificationType.ConflictDetected => 3,
                NotificationType.DriverLeaveRequest => 2,
                NotificationType.ReplacementSuggestion => 2,
                NotificationType.SystemAlert => 3,
                _ => 1
            };
        }

        private DateTime? GetNotificationExpiration(NotificationType type)
        {
            return type switch
            {
                NotificationType.DriverLeaveRequest => DateTime.UtcNow.AddDays(7),
                NotificationType.ReplacementSuggestion => DateTime.UtcNow.AddDays(7),
                NotificationType.ConflictDetected => DateTime.UtcNow.AddDays(3),
                NotificationType.SystemAlert => DateTime.UtcNow.AddDays(1),
                NotificationType.LeaveApproval => DateTime.UtcNow.AddDays(14),
                NotificationType.EmergencyNotification => DateTime.UtcNow.AddDays(1),
                _ => DateTime.UtcNow.AddDays(30) // Default expiration
            };
        }

        public async Task<int> GetAdminUnreadCountAsync()
        {
            try
            {
                var repository = GetRepository();
                var filter = Builders<Notification>.Filter.And(
                    Builders<Notification>.Filter.Eq(n => n.RecipientType, RecipientType.Admin),
                    Builders<Notification>.Filter.Eq(n => n.Status, NotificationStatus.Unread)
                );
                var notifications = await repository.FindByFilterAsync(filter);
                return notifications.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin unread count");
                return 0;
            }
        }

        
        public async Task<NotificationResponse?> GetNotificationByMetadataAsync(Guid userId, string relatedEntityType, string metadataKey)
        {
            try
            {
                var repository = GetRepository();
                var filter = Builders<Notification>.Filter.And(
                    Builders<Notification>.Filter.Eq(n => n.UserId, userId),
                    Builders<Notification>.Filter.Eq(n => n.RelatedEntityType, relatedEntityType),
                    Builders<Notification>.Filter.Eq("metadata.notificationKey", metadataKey)
                );

                var notification = await repository.FindByFilterAsync(filter);
                var firstNotification = notification.FirstOrDefault();

                return firstNotification != null ? _mapper.Map<NotificationResponse>(firstNotification) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification by metadata for user {UserId}", userId);
                return null;
            }
        }
    }
}
