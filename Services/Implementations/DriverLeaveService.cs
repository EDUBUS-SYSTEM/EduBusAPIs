using AutoMapper;
using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using Services.Contracts;
using Services.Models.Driver;
using Services.Models.Notification;
using Microsoft.Extensions.Logging;
using Utils;
using Microsoft.EntityFrameworkCore;
using Services.Models.Common;
using Constants;
using Route = Data.Models.Route;
namespace Services.Implementations
{
    public class DriverLeaveService : IDriverLeaveService
    {
        private readonly IDriverLeaveRepository _leaveRepo;
        private readonly IDriverRepository _driverRepo;
        private readonly IDriverVehicleRepository _driverVehicleRepo;
        private readonly INotificationService _notificationService;
        private readonly IConfigurationService _configurationService;
        private readonly ITripService _tripService;
        private readonly IDatabaseFactory _databaseFactory;
        private readonly IEmailService _emailService;
        private readonly IMapper _mapper;
        private readonly ILogger<DriverLeaveService> _logger;

        public DriverLeaveService(
            IDriverLeaveRepository leaveRepo,
            IDriverRepository driverRepo,
            IDriverVehicleRepository driverVehicleRepo,
            INotificationService notificationService,
            IConfigurationService configurationService,
            ITripService tripService,
            IDatabaseFactory databaseFactory,
            IEmailService emailService,
            IMapper mapper,
            ILogger<DriverLeaveService> logger)
        {
            _leaveRepo = leaveRepo;
            _driverRepo = driverRepo;
            _driverVehicleRepo = driverVehicleRepo;
            _notificationService = notificationService;
            _configurationService = configurationService;
            _tripService = tripService;
            _databaseFactory = databaseFactory;
            _emailService = emailService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<DriverLeaveResponse> CreateLeaveRequestAsync(CreateLeaveRequestDto dto)
        {
            var driver = await _driverRepo.FindAsync(dto.DriverId);
            if (driver == null || driver.IsDeleted) 
            {
                throw new KeyNotFoundException(
                    $"Driver with ID {dto.DriverId} not found.");
            }

            // Check if driver has active vehicle assignment
            var activeAssignments = await _driverVehicleRepo.GetActiveAssignmentsByDriverAsync(dto.DriverId);
             if (!activeAssignments.Any())
             {
                 _logger.LogWarning("Leave request rejected: Driver has no active vehicle assignment. Driver: {DriverId}", dto.DriverId);
                 throw new InvalidOperationException("Driver must have an active vehicle assignment before requesting leave.");
             }

            // Normalize start/end to day boundaries
            var startOfDay = dto.StartDate.Date;                 // 00:00:00
            var endOfDay = dto.EndDate.Date.AddDays(1).AddTicks(-1); // 23:59:59.9999999

            // Validate start date is not in the past
            var today = DateTime.UtcNow.Date;
            if (startOfDay < today)
            {
                _logger.LogWarning("Leave request rejected: Start date {StartDate} is in the past. Current date: {CurrentDate}",
                    dto.StartDate, today);
                throw new InvalidOperationException($"Leave start date cannot be in the past. Start date: {startOfDay:yyyy-MM-dd}, Current date: {today:yyyy-MM-dd}");
            }
            if (endOfDay < startOfDay)
            {
                throw new InvalidOperationException("Leave end date cannot be before start date.");
            }
            var settings = _configurationService.GetLeaveRequestSettings();
            var now = DateTime.UtcNow;
            var minimumAdvanceTime = now.AddHours(settings.MinimumAdvanceNoticeHours);
            var emergencyAdvanceTime = now.AddHours(settings.EmergencyLeaveAdvanceNoticeHours);          

            // Check if this is an emergency leave request
            var isEmergencyLeave = dto.LeaveType == LeaveType.Emergency;
            var isSickLeave = dto.LeaveType == LeaveType.Sick;
            var isUrgentLeave = isEmergencyLeave || isSickLeave;
            
            // Determine required advance time based on leave type and settings
            var requiredAdvanceTime = isEmergencyLeave && settings.AllowEmergencyLeaveRequests 
                ? emergencyAdvanceTime 
                : isSickLeave && settings.AllowEmergencyLeaveRequests
                ? emergencyAdvanceTime
                : minimumAdvanceTime;
            
            // Validate advance notice requirement
            if (startOfDay < requiredAdvanceTime)
            {
                var hoursRequired = isEmergencyLeave && settings.AllowEmergencyLeaveRequests 
                    ? settings.EmergencyLeaveAdvanceNoticeHours 
                    : isSickLeave && settings.AllowEmergencyLeaveRequests
                    ? settings.EmergencyLeaveAdvanceNoticeHours
                    : settings.MinimumAdvanceNoticeHours;
                    
                var leaveTypeText = isEmergencyLeave ? "emergency" : isSickLeave ? "sick" : "regular";
                var errorMessage = $"Leave start date must be at least {hoursRequired} hours in advance for {leaveTypeText} leave requests. " +
                    $"Current time: {now:yyyy-MM-dd HH:mm:ss}, Required start time: {requiredAdvanceTime:yyyy-MM-dd HH:mm:ss}";
                
                _logger.LogWarning("Leave request rejected: Insufficient advance notice. Driver: {DriverId}, LeaveType: {LeaveType}, " +
                    "StartDate: {StartDate}, RequiredAdvanceTime: {RequiredAdvanceTime}, HoursRequired: {HoursRequired}", 
                    dto.DriverId, dto.LeaveType, dto.StartDate, requiredAdvanceTime, hoursRequired);
                
                throw new InvalidOperationException(errorMessage);
            }

            // Check for overlapping leave requests with normalized dates
            var hasOverlappingLeave = await _leaveRepo.HasOverlappingLeaveAsync(
                dto.DriverId, startOfDay, endOfDay);
            if (hasOverlappingLeave)
            {
                // Get detailed information about overlapping leaves
                var overlappingLeaves = await _leaveRepo.GetLeavesByDriverAndDateRangeAsync(dto.DriverId, startOfDay, endOfDay);
                var activeOverlappingLeaves = overlappingLeaves.Where(l => l.Status == LeaveStatus.Pending || l.Status == LeaveStatus.Approved).ToList();
                
                if (activeOverlappingLeaves.Any())
                {
                    var overlappingLeave = activeOverlappingLeaves.First();
                    var statusText = overlappingLeave.Status == LeaveStatus.Pending ? "pending" : "approved";
                    
                    _logger.LogWarning("Leave request rejected: Overlapping leave exists. Driver: {DriverId}, " +
                        "Requested: {StartDate} to {EndDate}, Existing: {ExistingStartDate} to {ExistingEndDate}, Status: {Status}", 
                        dto.DriverId, dto.StartDate, dto.EndDate, overlappingLeave.StartDate, overlappingLeave.EndDate, overlappingLeave.Status);
                    
                    throw new InvalidOperationException(
                        $"You already have a {statusText} leave request from {overlappingLeave.StartDate:yyyy-MM-dd} to {overlappingLeave.EndDate:yyyy-MM-dd}. " +
                        "Please cancel or modify your existing request before creating a new one.");
                }
            }
            
            var entity = _mapper.Map<DriverLeaveRequest>(dto);
            entity.Id = Guid.NewGuid();
            entity.RequestedAt = DateTime.UtcNow;
            entity.Status = LeaveStatus.Pending;
            entity.StartDate = startOfDay;
            entity.EndDate = endOfDay;
            var created = await _leaveRepo.AddAsync(entity);
            
            // Send notification to admin about new leave request
            try
            {
                var message = $"New leave request from {driver?.FirstName} {driver?.LastName} for {dto.LeaveType} from {startOfDay:yyyy-MM-dd} to {endOfDay:yyyy-MM-dd}";
                
                var metadata = new Dictionary<string, object>
                {
                    ["leaveRequestId"] = created.Id,
                    ["driverId"] = dto.DriverId,
                    ["driverName"] = $"{driver?.FirstName} {driver?.LastName}",
                    ["leaveType"] = dto.LeaveType.ToString(),
                    ["startDate"] = dto.StartDate,
                    ["endDate"] = dto.EndDate,
                    ["reason"] = dto.Reason
                };
                
                await _notificationService.CreateDriverLeaveNotificationAsync(
                    created.Id, 
                    NotificationType.DriverLeaveRequest, 
                    message, 
                    metadata
                );
                
                _logger.LogInformation("Created leave request notification for leave {LeaveId}", created.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification for leave request {LeaveId}", created.Id);
                // Don't fail the leave request creation if notification fails
            }
            
            return _mapper.Map<DriverLeaveResponse>(created);
        }

        public async Task<DriverLeaveResponse> ApproveLeaveRequestAsync(Guid leaveId, ApproveLeaveRequestDto dto, Guid adminId)
        {
            var entity = await _leaveRepo.FindAsync(leaveId);
            if (entity == null || entity.IsDeleted) throw new InvalidOperationException("Leave not found");
            
            // Validate that leave request is in Pending status
            if (entity.Status != LeaveStatus.Pending)
            {
                throw new InvalidOperationException($"Cannot approve leave request. Current status: {entity.Status}. Only pending leave requests can be approved.");
            }
            
            // Validate that approval is before the leave start date
            var today = DateTime.UtcNow.Date;
            if (entity.StartDate <= today)
            {
                throw new InvalidOperationException($"Cannot approve leave request. Leave start date ({entity.StartDate:yyyy-MM-dd}) must be after today ({today:yyyy-MM-dd}).");
            }
            
            if (dto.ReplacementDriverId.HasValue)
            {
                await ProcessReplacementDriverAssignmentAsync(entity, dto.ReplacementDriverId.Value, adminId);
            }
            
            entity.Status = LeaveStatus.Approved;
            entity.ApprovedAt = DateTime.UtcNow;
            entity.ApprovedByAdminId = adminId;
            entity.ApprovalNote = dto.Notes;
            var updated = await _leaveRepo.UpdateAsync(entity);
            
            try
            {
                var driver = await _driverRepo.FindAsync(entity.DriverId);
                var statusText = entity.Status.ToString();
                var message = $"Your leave request for {entity.LeaveType} from {entity.StartDate:yyyy-MM-dd} to {entity.EndDate:yyyy-MM-dd} has been {statusText}.";
                
                if (!string.IsNullOrEmpty(dto.Notes))
                {
                    message += $" Note: {dto.Notes}";
                }
                
                if (dto.ReplacementDriverId.HasValue)
                {
                    var replacementDriver = await _driverRepo.FindAsync(dto.ReplacementDriverId.Value);
                    message += $" Replacement driver: {replacementDriver?.FirstName} {replacementDriver?.LastName}";
                }
                
                var metadata = new Dictionary<string, object>
                {
                    ["leaveRequestId"] = entity.Id.ToString(),
                    ["driverId"] = entity.DriverId.ToString(),
                    ["adminId"] = adminId.ToString(),
                    ["status"] = entity.Status.ToString(),
                    ["approvalNote"] = dto.Notes ?? "",
                    ["replacementDriverId"] = dto.ReplacementDriverId?.ToString() ?? ""
                };

                var approvalNotification = new CreateNotificationDto
                {
                    UserId = entity.DriverId,
                    Title = "Leave request update",
                    Message = message,
                    NotificationType = NotificationType.LeaveApproval,
                    RecipientType = RecipientType.Driver,
                    Priority = 2,
                    RelatedEntityId = entity.Id,
                    RelatedEntityType = "DriverLeaveRequest",
                    ActionRequired = false,
                    ActionUrl = null,
                    ExpiresAt = DateTime.UtcNow.AddDays(30),
                    Metadata = metadata
                };

                _logger.LogInformation("Sending leave approval notification to driver {DriverId} for leave {LeaveId}", entity.DriverId, entity.Id);
                await _notificationService.CreateNotificationAsync(approvalNotification);
                _logger.LogInformation("Sent leave approval notification to driver {DriverId} for leave {LeaveId}", entity.DriverId, entity.Id);
                _logger.LogInformation("Created leave approval notification for leave {LeaveId}", entity.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification for leave approval {LeaveId}", entity.Id);
            }
            
            return _mapper.Map<DriverLeaveResponse>(updated!);
        }

        public async Task<DriverLeaveResponse> RejectLeaveRequestAsync(Guid leaveId, RejectLeaveRequestDto dto, Guid adminId)
        {
            var entity = await _leaveRepo.FindAsync(leaveId);
            if (entity == null || entity.IsDeleted) throw new InvalidOperationException("Leave not found");
            
            // Validate that leave request is in Pending status
            if (entity.Status != LeaveStatus.Pending)
            {
                throw new InvalidOperationException($"Cannot reject leave request. Current status: {entity.Status}. Only pending leave requests can be rejected.");
            }
            
            entity.Status = LeaveStatus.Rejected;
            entity.ApprovedAt = DateTime.UtcNow;
            entity.ApprovedByAdminId = adminId;
            entity.ApprovalNote = dto.Reason;
            var updated = await _leaveRepo.UpdateAsync(entity);
            
            // Send notification to driver about rejection
            try
            {
                var driver = await _driverRepo.FindAsync(entity.DriverId);
                var message = $"Your leave request for {entity.LeaveType} from {entity.StartDate:yyyy-MM-dd} to {entity.EndDate:yyyy-MM-dd} has been rejected.";
                
                if (!string.IsNullOrEmpty(dto.Reason))
                {
                    message += $" Reason: {dto.Reason}";
                }
                
                var metadata = new Dictionary<string, object>
                {
                    ["leaveRequestId"] = entity.Id.ToString(),
                    ["driverId"] = entity.DriverId.ToString(),
                    ["adminId"] = adminId.ToString(),
                    ["rejectionReason"] = dto.Reason
                };

                var rejectionNotification = new CreateNotificationDto
                {
                    UserId = entity.DriverId,
                    Title = "Leave request update",
                    Message = message,
                    NotificationType = NotificationType.LeaveApproval,
                    RecipientType = RecipientType.Driver,
                    Priority = 2,
                    RelatedEntityId = entity.Id,
                    RelatedEntityType = "DriverLeaveRequest",
                    ActionRequired = false,
                    ActionUrl = null,
                    ExpiresAt = DateTime.UtcNow.AddDays(30),
                    Metadata = metadata
                };

                _logger.LogInformation("Sending leave rejection notification to driver {DriverId} for leave {LeaveId}", entity.DriverId, entity.Id);
                await _notificationService.CreateNotificationAsync(rejectionNotification);
                _logger.LogInformation("Sent leave rejection notification to driver {DriverId} for leave {LeaveId}", entity.DriverId, entity.Id);
                
                _logger.LogInformation("Created leave rejection notification for leave {LeaveId}", entity.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification for leave rejection {LeaveId}", entity.Id);
            }
            
            return _mapper.Map<DriverLeaveResponse>(updated!);
        }

        public async Task<ReplacementSuggestionResponse> GenerateReplacementSuggestionsAsync(Guid leaveId)
        {
            var leave = await _leaveRepo.FindAsync(leaveId) ?? throw new InvalidOperationException("Leave not found");
            var suggestions = new List<ReplacementSuggestionDto>();
            // naive: any available driver and vehicle for the first day window
            var start = leave.StartDate;
            var end = leave.EndDate;
            var availableDrivers = await _driverVehicleRepo.FindAvailableDriversAsync(start, end, null);
            var availableVehicles = await _driverVehicleRepo.FindAvailableVehiclesAsync(start, end, 0);
            foreach (var d in availableDrivers.Take(3))
            {
                var v = availableVehicles.FirstOrDefault();
                if (v == null) break;
                suggestions.Add(new ReplacementSuggestionDto
                {
                    Id = Guid.NewGuid(),
                    DriverId = d.Id,
                    DriverName = $"{d.FirstName} {d.LastName}",
                    DriverEmail = d.Email,
                    DriverPhone = d.PhoneNumber,
                    VehicleId = v.Id,
                    VehiclePlate = SecurityHelper.DecryptFromBytes(v.HashedLicensePlate),
                    VehicleCapacity = v.Capacity,
                    Score = 50,
                    Reason = "Available in time window",
                    GeneratedAt = DateTime.UtcNow,
                    HasValidLicense = d.DriverLicense != null,
                    HasHealthCertificate = d.HealthCertificateFileId.HasValue,
                    YearsOfExperience = d.DriverLicense != null ? (int)Math.Max(0, (DateTime.UtcNow - d.DriverLicense.DateOfIssue).TotalDays / 365) : 0,
                    IsAvailable = true
                });
            }
            return new ReplacementSuggestionResponse
            {
                Success = true,
                Suggestions = suggestions,
                TotalSuggestions = suggestions.Count,
                Message = $"Generated {suggestions.Count} suggestions"
            };
        }

        public async Task<IEnumerable<DriverLeaveResponse>> GetDriverLeavesAsync(Guid driverId, DateTime? fromDate, DateTime? toDate)
        {
            var list = await _leaveRepo.GetByDriverIdAsync(driverId, fromDate, toDate);
            return list.Select(_mapper.Map<DriverLeaveResponse>);
        }

        public async Task<DriverLeaveListResponse> GetDriverLeavesPaginatedAsync(
            Guid driverId, 
            DateTime? fromDate, 
            DateTime? toDate, 
            LeaveStatus? status,
            int page, 
            int perPage)
        {
            try
            {
                // Validate pagination parameters
                if (page < 1) page = 1;
                if (perPage < 1 || perPage > 100) perPage = 20;

                // Get paginated data from repository
                var (items, totalCount) = await _leaveRepo.GetByDriverIdPaginatedAsync(
                    driverId, fromDate, toDate, status, page, perPage);

                // Calculate pagination info
                var totalPages = (int)Math.Ceiling((double)totalCount / perPage);
                var pagination = new PaginationInfo
                {
                    CurrentPage = page,
                    PerPage = perPage,
                    TotalItems = totalCount,
                    TotalPages = totalPages,
                    HasNextPage = page < totalPages,
                    HasPreviousPage = page > 1
                };

                // Map to response DTOs
                var leaveResponses = items.Select(l => _mapper.Map<DriverLeaveResponse>(l)).ToList();

                // Get pending leaves count for this driver
                var pendingCount = await _leaveRepo.GetQueryable()
                    .Where(l => l.DriverId == driverId && !l.IsDeleted && l.Status == LeaveStatus.Pending)
                    .CountAsync();

                return new DriverLeaveListResponse
                {
                    Success = true,
                    Data = leaveResponses,
                    Pagination = pagination,
                    PendingLeavesCount = pendingCount
                };
            }
            catch (Exception ex)
            {
                return new DriverLeaveListResponse
                {
                    Success = false,
                    Error = new { message = "An error occurred while retrieving leave requests.", details = ex.Message }
                };
            }
        }

        public async Task<DriverLeaveResponse?> GetLeaveByIdAsync(Guid leaveId)
        {
            var entity = await _leaveRepo.FindAsync(leaveId);
            return entity == null ? null : _mapper.Map<DriverLeaveResponse>(entity);
        }

        public async Task<DriverLeaveListResponse> GetLeaveRequestsAsync(DriverLeaveListRequest request)
        {
            try
            {
                // Build query with filters
                var query = _leaveRepo.GetQueryable()
                    .Include(l => l.Driver)
                    .ThenInclude(d => d.DriverLicense)
                    .Include(l => l.ApprovedByAdmin)
                    .Where(l => !l.IsDeleted);

                // Apply search filter
                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    var searchTerm = request.SearchTerm.ToLower();
                    query = query.Where(l =>
                        l.Driver.Email.ToLower().Contains(searchTerm) ||
                        l.Driver.FirstName.ToLower().Contains(searchTerm) ||
                        l.Driver.LastName.ToLower().Contains(searchTerm) ||
                        (l.Driver.FirstName + " " + l.Driver.LastName).ToLower().Contains(searchTerm));
                }

                // Apply status filter
                if (request.Status.HasValue)
                {
                    query = query.Where(l => l.Status == request.Status.Value);
                }

                // Apply leave type filter
                if (request.LeaveType.HasValue)
                {
                    query = query.Where(l => l.LeaveType == request.LeaveType.Value);
                }

                // Get total count for pagination
                var totalItems = await query.CountAsync();

                // Apply sorting - chỉ có 2 option: gần nhất (desc) hoặc xa nhất (asc)
                query = request.SortOrder.ToLower() switch
                {
                    "asc" => query.OrderBy(l => l.RequestedAt),      // Xa nhất
                    "desc" => query.OrderByDescending(l => l.RequestedAt), // Gần nhất (mặc định)
                    _ => query.OrderByDescending(l => l.RequestedAt) // Default: gần nhất
                };

                // Apply pagination
                var skip = (request.Page - 1) * request.PerPage;
                var leaves = await query
                    .Skip(skip)
                    .Take(request.PerPage)
                    .ToListAsync();

                // Calculate pagination info
                var totalPages = (int)Math.Ceiling((double)totalItems / request.PerPage);
                var pagination = new PaginationInfo
                {
                    CurrentPage = request.Page,
                    PerPage = request.PerPage,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    HasNextPage = request.Page < totalPages,
                    HasPreviousPage = request.Page > 1
                };
                
                var pendingLeavesCount = await _leaveRepo.GetQueryable()
                    .Where(l => !l.IsDeleted && l.Status == LeaveStatus.Pending)
                    .CountAsync();

                // Get primary vehicles for all drivers in the result
                var driverIds = leaves.Select(l => l.DriverId).Distinct().ToList();
                var primaryVehicles = await _driverVehicleRepo.GetPrimaryVehiclesForDriversAsync(driverIds);

                // Map to response DTOs with vehicle information
                var leaveResponses = leaves.Select(l => {
                    var response = _mapper.Map<DriverLeaveResponse>(l);

                    // Add primary vehicle information
                    var primaryVehicle = primaryVehicles.GetValueOrDefault(l.DriverId);
                    if (primaryVehicle != null)
                    {
                        response.DriverLicenseNumber = l.Driver.DriverLicense != null && !l.Driver.DriverLicense.IsDeleted ? SecurityHelper.DecryptFromBytes(l.Driver.DriverLicense.HashedLicenseNumber) : null;
                        response.PrimaryVehicleId = primaryVehicle.VehicleId;
                        response.PrimaryVehicleLicensePlate = SecurityHelper.DecryptFromBytes(primaryVehicle.Vehicle.HashedLicensePlate);
                    }

                    return response;
                }).ToList();


                return new DriverLeaveListResponse
                {
                    Success = true,
                    Data = leaveResponses,
                    Pagination = pagination,
                    PendingLeavesCount = pendingLeavesCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving leave requests with pagination");
                return new DriverLeaveListResponse
                {
                    Success = false,
                    Error = new { message = "An error occurred while retrieving leave requests.", details = ex.Message }
                };
            }
        }

        public async Task<DriverLeaveResponse?> GetActiveReplacementInfoByDriverIdAsync(Guid driverId)
        {
            try
            {
                // Get replacement info
                var leaveRequest = await _leaveRepo.GetActiveReplacementByDriverIdAsync(driverId);

                if (leaveRequest == null)
                    return null;

                // Map to response
                return new DriverLeaveResponse
                {
                    Id = leaveRequest.Id,
                    DriverId = leaveRequest.DriverId,
                    DriverName = $"{leaveRequest.Driver.FirstName} {leaveRequest.Driver.LastName}",
                    DriverEmail = leaveRequest.Driver.Email,
                    DriverPhoneNumber = leaveRequest.Driver.PhoneNumber ?? string.Empty,
                    DriverLicenseNumber = leaveRequest.Driver.DriverLicense != null 
                        ? SecurityHelper.DecryptFromBytes(leaveRequest.Driver.DriverLicense.HashedLicenseNumber)
                        : string.Empty,
                    LeaveType = leaveRequest.LeaveType,
                    StartDate = leaveRequest.StartDate,
                    EndDate = leaveRequest.EndDate,
                    Reason = leaveRequest.Reason,
                    Status = leaveRequest.Status,
                    RequestedAt = leaveRequest.RequestedAt,
                    ApprovedByAdminId = leaveRequest.ApprovedByAdminId,
                    ApprovedByAdminName = leaveRequest.ApprovedByAdmin != null 
                        ? $"{leaveRequest.ApprovedByAdmin.FirstName} {leaveRequest.ApprovedByAdmin.LastName}"
                        : null,
                    ApprovedAt = leaveRequest.ApprovedAt,
                    ApprovalNote = leaveRequest.ApprovalNote,
                    AutoReplacementEnabled = leaveRequest.AutoReplacementEnabled,
                    SuggestedReplacementDriverId = leaveRequest.SuggestedReplacementDriverId,
                    SuggestedReplacementDriverName = leaveRequest.SuggestedReplacementDriver != null
                        ? $"{leaveRequest.SuggestedReplacementDriver.FirstName} {leaveRequest.SuggestedReplacementDriver.LastName}"
                        : null,
                    SuggestedReplacementVehicleId = leaveRequest.SuggestedReplacementVehicleId,
                    SuggestedReplacementVehiclePlate = leaveRequest.SuggestedReplacementVehicle != null
                        ? SecurityHelper.DecryptFromBytes(leaveRequest.SuggestedReplacementVehicle.HashedLicensePlate)
                        : null,
                    SuggestionGeneratedAt = leaveRequest.SuggestionGeneratedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active replacement info for driver {DriverId}", driverId);
                return null;
            }
        }

        public async Task<IEnumerable<DriverReplacementMatchDto>> GetActiveReplacementMatchesAsync()
        {
            try
            {
                var activeReplacements = await _leaveRepo.GetActiveReplacementsAsync();
                
                return activeReplacements.Select(lr => new DriverReplacementMatchDto
                {
                    DriverId = lr.DriverId,
                    VehicleId = lr.SuggestedReplacementVehicleId,
                    StartDate = lr.StartDate,
                    EndDate = lr.EndDate
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active replacement matches");
                return new List<DriverReplacementMatchDto>();
            }
        }

        private async Task ProcessReplacementDriverAssignmentAsync(DriverLeaveRequest leaveRequest, Guid replacementDriverId, Guid adminId)
        {
            leaveRequest.SuggestedReplacementDriverId = replacementDriverId;
            leaveRequest.SuggestionGeneratedAt = DateTime.UtcNow;
            
            var replacementDriver = await _driverRepo.FindAsync(replacementDriverId);
            if (replacementDriver == null || replacementDriver.IsDeleted)
            {
                throw new InvalidOperationException("Replacement driver not found.");
            }
            
            var primaryVehicle = await _driverVehicleRepo.GetPrimaryVehicleForDriverAsync(leaveRequest.DriverId);
            if (primaryVehicle == null)
            {
                throw new InvalidOperationException("Driver has no primary vehicle assignment.");
            }
            
            var createdAssignment = await CreateTemporaryDriverVehicleAssignmentAsync(
                replacementDriverId, 
                primaryVehicle.VehicleId, 
                leaveRequest.StartDate, 
                leaveRequest.EndDate, 
                adminId,
                leaveRequest.Id);
            
            leaveRequest.SuggestedReplacementVehicleId = primaryVehicle.VehicleId;
            
            await UpdateTripsWithReplacementDriverAsync(
                primaryVehicle.VehicleId, 
                leaveRequest.StartDate, 
                leaveRequest.EndDate, 
                createdAssignment.Id, 
                replacementDriver);
            
            await SendReplacementDriverNotificationAsync(leaveRequest, replacementDriver, primaryVehicle.VehicleId);
            
            await SendReplacementDriverEmailAsync(replacementDriver, leaveRequest.StartDate, leaveRequest.EndDate);
        }

        private async Task<DriverVehicle> CreateTemporaryDriverVehicleAssignmentAsync(
            Guid driverId, 
            Guid vehicleId, 
            DateTime startDate, 
            DateTime endDate, 
            Guid adminId,
            Guid leaveRequestId)
        {
            var startTime = startDate.Date;
            var endTime = endDate.Date.AddDays(1).AddTicks(-1);
            
            var tempAssignment = new DriverVehicle
            {
                Id = Guid.NewGuid(),
                DriverId = driverId,
                VehicleId = vehicleId,
                IsPrimaryDriver = false,
                StartTimeUtc = startTime,
                EndTimeUtc = endTime,
                Status = DriverVehicleStatus.Assigned,
                AssignedByAdminId = adminId,
                AssignmentReason = $"Temporary replacement for driver on leave from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}"
            };
            
            var createdAssignment = await _driverVehicleRepo.AssignDriverAsync(tempAssignment);
            _logger.LogInformation("Created temporary DriverVehicle assignment {AssignmentId} for replacement driver {DriverId}", 
                createdAssignment.Id, driverId);
            
            return createdAssignment;
        }

        private async Task UpdateTripsWithReplacementDriverAsync(
            Guid vehicleId, 
            DateTime startDate, 
            DateTime endDate, 
            Guid driverVehicleId, 
            Driver replacementDriver)
        {
            try
            {
                var tripRepo = _databaseFactory.GetRepositoryByType<ITripRepository>(DatabaseType.MongoDb);
                var routeRepo = _databaseFactory.GetRepositoryByType<IMongoRepository<Route>>(DatabaseType.MongoDb);
                
                _logger.LogInformation("Searching trips for vehicle {VehicleId} from {StartDate} to {EndDate}", 
                    vehicleId, startDate, endDate);
                
                var allTrips = new List<Trip>();
                
                var tripsByVehicle = await tripRepo.GetTripsByVehicleAndDateRangeAsync(vehicleId, startDate, endDate);
                allTrips.AddRange(tripsByVehicle);
                
                var routes = await routeRepo.FindByConditionAsync(r => r.VehicleId == vehicleId && r.IsActive && !r.IsDeleted);
                foreach (var route in routes)
                {
                    var routeTrips = await tripRepo.GetTripsByRouteAsync(route.Id);
                    var filteredRouteTrips = routeTrips.Where(t => 
                        !t.IsDeleted && 
                        t.ServiceDate.Date >= startDate.Date && 
                        t.ServiceDate.Date <= endDate.Date);
                    allTrips.AddRange(filteredRouteTrips);
                }
                
                var uniqueTrips = allTrips
                    .Where(t => !t.IsDeleted && 
                                t.ServiceDate.Date >= startDate.Date && 
                                t.ServiceDate.Date <= endDate.Date)
                    .GroupBy(t => t.Id)
                    .Select(g => g.First())
                    .ToList();
                
                _logger.LogInformation("Found {Count} unique trips for vehicle {VehicleId} in date range", uniqueTrips.Count, vehicleId);
                
                var updatedCount = 0;
                foreach (var trip in uniqueTrips)
                {
                    _logger.LogInformation("Updating trip {TripId} (ServiceDate: {ServiceDate}, RouteId: {RouteId}, CurrentDriverVehicleId: {CurrentDriverVehicleId}) with replacement driver {DriverId} (DriverVehicleId: {DriverVehicleId})", 
                        trip.Id, trip.ServiceDate, trip.RouteId, trip.DriverVehicleId, replacementDriver.Id, driverVehicleId);
                    
                    var originalDriverVehicleId = trip.DriverVehicleId;
                    
                    trip.DriverVehicleId = driverVehicleId;
                    trip.Driver = new Trip.DriverSnapshot
                    {
                        Id = replacementDriver.Id,
                        FullName = $"{replacementDriver.FirstName} {replacementDriver.LastName}".Trim(),
                        Phone = replacementDriver.PhoneNumber ?? string.Empty,
                        IsPrimary = false,
                        SnapshottedAtUtc = DateTime.UtcNow
                    };
                    
                    var updatedTrip = await _tripService.UpdateTripAsync(trip);
                    if (updatedTrip != null)
                    {
                        if (updatedTrip.DriverVehicleId == driverVehicleId && updatedTrip.Driver?.Id == replacementDriver.Id)
                        {
                            updatedCount++;
                            _logger.LogInformation("Successfully updated trip {TripId} with replacement driver {DriverId} (DriverVehicleId: {DriverVehicleId})", 
                                trip.Id, replacementDriver.Id, driverVehicleId);
                        }
                        else
                        {
                            _logger.LogWarning("Trip {TripId} was updated but driver info doesn't match. Expected DriverVehicleId: {ExpectedDriverVehicleId}, Got: {ActualDriverVehicleId}. Expected DriverId: {ExpectedDriverId}, Got: {ActualDriverId}", 
                                trip.Id, driverVehicleId, updatedTrip.DriverVehicleId, replacementDriver.Id, updatedTrip.Driver?.Id);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Failed to update trip {TripId} - UpdateTripAsync returned null", trip.Id);
                    }
                }
                
                _logger.LogInformation("Updated {Count} out of {Total} trips with replacement driver {DriverId}", 
                    updatedCount, uniqueTrips.Count, replacementDriver.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating trips with replacement driver {DriverId}", replacementDriver.Id);
                throw;
            }
        }

        private async Task SendReplacementDriverNotificationAsync(
            DriverLeaveRequest leaveRequest, 
            Driver replacementDriver, 
            Guid vehicleId)
        {
            try
            {
                var messageEn = $"You have been assigned as a replacement driver from {leaveRequest.StartDate:yyyy-MM-dd} to {leaveRequest.EndDate:yyyy-MM-dd}. Please check your work schedule.";
                
                var notificationMetadata = new Dictionary<string, object>
                {
                    ["leaveRequestId"] = leaveRequest.Id.ToString(),
                    ["replacementDriverId"] = replacementDriver.Id.ToString(),
                    ["startDate"] = leaveRequest.StartDate,
                    ["endDate"] = leaveRequest.EndDate,
                    ["vehicleId"] = vehicleId.ToString()
                };
                
                _logger.LogInformation("Creating notification for replacement driver {DriverId} (UserId: {UserId}) at email {Email}", 
                    replacementDriver.Id, replacementDriver.Id, replacementDriver.Email);
                
                var notificationDto = new CreateNotificationDto
                {
                    UserId = replacementDriver.Id,
                    Title = "Replacement assignment",
                    Message = messageEn,
                    NotificationType = NotificationType.ReplacementTrip,
                    RecipientType = RecipientType.Driver,
                    Priority = 2,
                    RelatedEntityId = leaveRequest.Id,
                    RelatedEntityType = "DriverLeaveRequest",
                    ActionRequired = false,
                    ActionUrl = null,
                    ExpiresAt = DateTime.UtcNow.AddDays(30),
                    Metadata = notificationMetadata
                };
                
                _logger.LogInformation("Sending replacement trip notification to driver {DriverId} for leave {LeaveId}", replacementDriver.Id, leaveRequest.Id);
                await _notificationService.CreateNotificationAsync(notificationDto);
                _logger.LogInformation("Sent replacement trip notification to driver {DriverId} for leave {LeaveId}", replacementDriver.Id, leaveRequest.Id);
                
                _logger.LogInformation("Successfully sent notification to replacement driver {DriverId} (UserId: {UserId})", 
                    replacementDriver.Id, replacementDriver.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to replacement driver {DriverId} (UserId: {UserId})", 
                    replacementDriver.Id, replacementDriver.Id);
            }
        }

        private async Task SendReplacementDriverEmailAsync(Driver replacementDriver, DateTime startDate, DateTime endDate)
        {
            try
            {
                var emailSubject = "Phân công tài xế thay thế | Replacement Driver Assignment";
                var (_, emailBody) = CreateReplacementDriverEmailTemplate(
                    replacementDriver.FirstName, 
                    replacementDriver.LastName, 
                    startDate, 
                    endDate);

                await _emailService.SendEmailAsync(replacementDriver.Email, emailSubject, emailBody);
                _logger.LogInformation("Sent email to replacement driver {DriverId} at {Email}", replacementDriver.Id, replacementDriver.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to replacement driver {DriverId}", replacementDriver.Id);
            }
        }

        private (string subject, string body) CreateReplacementDriverEmailTemplate(string firstName, string lastName, DateTime startDate, DateTime endDate)
        {
            var subject = "Phân công tài xế thay thế | Replacement Driver Assignment";
            
            var body = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset=""UTF-8"">
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
            </head>
            <body style=""margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f5f5f5;"">
                <div style=""max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 20px;"">
                    <h2 style=""color: #2E7D32; margin-top: 0;"">🚌 Phân công tài xế thay thế</h2>
        
                    <p>Kính gửi <strong>{firstName} {lastName}</strong>,</p>
        
                    <p>Bạn đã được chỉ định làm <strong>tài xế thay thế</strong> từ <strong>{startDate:dd/MM/yyyy}</strong> đến <strong>{endDate:dd/MM/yyyy}</strong>.</p>
        
                    <div style=""background-color: #E8F5E8; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #2E7D32;"">
                        <h3 style=""color: #2E7D32; margin-top: 0;"">📅 Thông tin phân công:</h3>
                        <p style=""margin: 10px 0;""><strong>Ngày bắt đầu:</strong> {startDate:dd/MM/yyyy}</p>
                        <p style=""margin: 10px 0;""><strong>Ngày kết thúc:</strong> {endDate:dd/MM/yyyy}</p>
                        <p style=""margin: 10px 0;""><strong>Vai trò:</strong> Tài xế thay thế</p>
                    </div>
        
                    <div style=""background-color: #FFF3E0; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #F57C00;"">
                        <h3 style=""color: #F57C00; margin-top: 0;"">📋 Hướng dẫn:</h3>
                        <p style=""margin: 10px 0;"">Vui lòng <strong>kiểm tra lịch làm việc</strong> của bạn trong ứng dụng EduBus để xem các chuyến xe được phân công trong khoảng thời gian này.</p>
                        <p style=""margin: 10px 0;"">Đảm bảo bạn đã nắm rõ lịch trình và sẵn sàng thực hiện các chuyến xe được giao.</p>
                    </div>
        
                    <p>Nếu bạn có bất kỳ câu hỏi nào, vui lòng liên hệ bộ phận quản lý.</p>
        
                    <p style=""margin-top: 30px;"">Trân trọng,<br>
                    <strong style=""color: #2E7D32;"">Đội ngũ quản lý EduBus</strong></p>
        
                    <hr style=""border: none; border-top: 1px solid #e0e0e0; margin: 30px 0;"">
        
                    <h2 style=""color: #2E7D32;"">🚌 Replacement Driver Assignment</h2>
        
                    <p>Dear <strong>{firstName} {lastName}</strong>,</p>
        
                    <p>You have been assigned as a <strong>replacement driver</strong> from <strong>{startDate:yyyy-MM-dd}</strong> to <strong>{endDate:yyyy-MM-dd}</strong>.</p>
        
                    <div style=""background-color: #E8F5E8; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #2E7D32;"">
                        <h3 style=""color: #2E7D32; margin-top: 0;"">📅 Assignment Information:</h3>
                        <p style=""margin: 10px 0;""><strong>Start Date:</strong> {startDate:yyyy-MM-dd}</p>
                        <p style=""margin: 10px 0;""><strong>End Date:</strong> {endDate:yyyy-MM-dd}</p>
                        <p style=""margin: 10px 0;""><strong>Role:</strong> Replacement Driver</p>
                    </div>
        
                    <div style=""background-color: #FFF3E0; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #F57C00;"">
                        <h3 style=""color: #F57C00; margin-top: 0;"">📋 Instructions:</h3>
                        <p style=""margin: 10px 0;"">Please <strong>check your work schedule</strong> in the EduBus app to view trip assignments during this period.</p>
                        <p style=""margin: 10px 0;"">Ensure you are familiar with the schedule and ready to perform the assigned trips.</p>
                    </div>
        
                    <p>If you have any questions, please contact the management team.</p>
        
                    <p style=""margin-top: 30px;"">Best regards,<br>
                    <strong style=""color: #2E7D32;"">EduBus Management Team</strong></p>
                </div>
            </body>
            </html>";
            return (subject, body);
        }
    }
}
