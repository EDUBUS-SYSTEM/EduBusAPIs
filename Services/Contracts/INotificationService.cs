using Services.Models.Notification;
using Data.Models.Enums;

namespace Services.Contracts
{
    public interface INotificationService
    {
        Task<NotificationResponse> CreateNotificationAsync(CreateNotificationDto dto);
        Task<NotificationResponse> GetNotificationByIdAsync(string id);
        Task<IEnumerable<NotificationResponse>> GetNotificationsByUserIdAsync(Guid userId, int page = 1, int pageSize = 20);
        Task<NotificationResponse> MarkAsReadAsync(string notificationId, Guid userId);
        Task<NotificationResponse> AcknowledgeNotificationAsync(string notificationId, Guid userId);
        Task<bool> DeleteNotificationAsync(string notificationId, Guid userId);
        Task<IEnumerable<NotificationResponse>> MarkAllAsReadAsync(Guid userId);
        Task<int> GetUnreadCountAsync(Guid userId);
        Task<int> GetAdminUnreadCountAsync();
        Task<IEnumerable<NotificationResponse>> GetNotificationsByTypeAsync(NotificationType type, int page = 1, int pageSize = 20);
        Task<NotificationResponse> CreateAdminNotificationAsync(CreateAdminNotificationDto dto);
        Task<IEnumerable<NotificationResponse>> GetAdminNotificationsAsync(int page = 1, int pageSize = 20);
        Task<IEnumerable<NotificationResponse>> GetNotificationsRequiringActionAsync();
        Task<NotificationResponse> CreateSystemNotificationAsync(CreateSystemNotificationDto dto);
        Task CleanupExpiredNotificationsAsync();
        Task<NotificationResponse> CreateDriverLeaveNotificationAsync(Guid leaveRequestId, NotificationType type, string message, Dictionary<string, object>? metadata = null);
        Task<NotificationResponse> CreateReplacementSuggestionNotificationAsync(Guid leaveRequestId, int suggestionCount, Dictionary<string, object>? metadata = null);
        Task<NotificationResponse?> GetNotificationByMetadataAsync(Guid userId, string relatedEntityType, string metadataKey);
    }
}
