using AutoMapper;
using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using Services.Contracts;
using Services.Models.Driver;
using Microsoft.Extensions.Logging;
using Utils;
using Microsoft.EntityFrameworkCore;
using Services.Models.Common;
namespace Services.Implementations
{
    public class DriverLeaveService : IDriverLeaveService
    {
        private readonly IDriverLeaveRepository _leaveRepo;
        private readonly IDriverLeaveConflictRepository _conflictRepo;
        private readonly IDriverRepository _driverRepo;
        private readonly IDriverVehicleRepository _driverVehicleRepo;
        private readonly INotificationService _notificationService;
        private readonly IConfigurationService _configurationService;
        private readonly IMapper _mapper;
        private readonly ILogger<DriverLeaveService> _logger;

        public DriverLeaveService(
            IDriverLeaveRepository leaveRepo,
            IDriverLeaveConflictRepository conflictRepo,
            IDriverRepository driverRepo,
            IDriverVehicleRepository driverVehicleRepo,
            INotificationService notificationService,
            IConfigurationService configurationService,
            IMapper mapper,
            ILogger<DriverLeaveService> logger)
        {
            _leaveRepo = leaveRepo;
            _conflictRepo = conflictRepo;
            _driverRepo = driverRepo;
            _driverVehicleRepo = driverVehicleRepo;
            _notificationService = notificationService;
            _configurationService = configurationService;
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

            // Validate start date is not in the past
            var today = DateTime.UtcNow.Date;
            if (dto.StartDate < today)
            {
                _logger.LogWarning("Leave request rejected: Start date {StartDate} is in the past. Current date: {CurrentDate}",
                    dto.StartDate, today);
                throw new InvalidOperationException($"Leave start date cannot be in the past. Start date: {dto.StartDate:yyyy-MM-dd}, Current date: {today:yyyy-MM-dd}");
            }
            if (dto.EndDate < dto.StartDate)
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
            if (dto.StartDate < requiredAdvanceTime)
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

            // Check for overlapping leave requests
            var normalizedStartDate = dto.StartDate.Date; // 00:00:00.000
            var normalizedEndDate = dto.EndDate.Date.AddDays(1).AddTicks(-1); // 23:59:59.999

            // Check for overlapping leave requests with normalized dates
            var hasOverlappingLeave = await _leaveRepo.HasOverlappingLeaveAsync(
                dto.DriverId, normalizedStartDate, normalizedEndDate);
            if (hasOverlappingLeave)
            {
                // Get detailed information about overlapping leaves
                var overlappingLeaves = await _leaveRepo.GetLeavesByDriverAndDateRangeAsync(dto.DriverId, dto.StartDate, dto.EndDate);
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
            var created = await _leaveRepo.AddAsync(entity);
            
            // Send notification to admin about new leave request
            try
            {
                var message = $"New leave request from {driver?.FirstName} {driver?.LastName} for {dto.LeaveType} from {dto.StartDate:yyyy-MM-dd} to {dto.EndDate:yyyy-MM-dd}";
                
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

        public async Task<DriverLeaveResponse> UpdateLeaveRequestAsync(Guid leaveId, UpdateLeaveRequestDto dto)
        {
            var entity = await _leaveRepo.FindAsync(leaveId);
            if (entity == null || entity.IsDeleted) throw new InvalidOperationException("Leave not found");
            
            // Check if the leave request is in a state that allows updates
            if (entity.Status != LeaveStatus.Pending)
            {
                throw new InvalidOperationException($"Cannot update leave request. Current status: {entity.Status}. Only pending leave requests can be updated.");
            }
            
            // Store original values for validation
            var originalStartDate = entity.StartDate;
            var originalEndDate = entity.EndDate;
            
            // Update fields
            if (dto.LeaveType.HasValue) entity.LeaveType = dto.LeaveType.Value;
            if (dto.StartDate.HasValue) entity.StartDate = dto.StartDate.Value;
            if (dto.EndDate.HasValue) entity.EndDate = dto.EndDate.Value;
            if (!string.IsNullOrWhiteSpace(dto.Reason)) entity.Reason = dto.Reason;
            if (dto.AutoReplacementEnabled.HasValue) entity.AutoReplacementEnabled = dto.AutoReplacementEnabled.Value;
            if (!string.IsNullOrWhiteSpace(dto.AdditionalInformation)) { /* store if later modeled */ }
            
            // Validate date changes if dates were modified
            if (dto.StartDate.HasValue || dto.EndDate.HasValue)
            {
                // Validate start date is not in the past
                var today = DateTime.UtcNow.Date;
                if (entity.StartDate < today)
                {
                    throw new InvalidOperationException($"Leave start date cannot be in the past. Start date: {entity.StartDate:yyyy-MM-dd}, Current date: {today:yyyy-MM-dd}");
                }
                
                if (entity.EndDate < entity.StartDate)
                {
                    throw new InvalidOperationException("Leave end date cannot be before start date.");
                }
                
                // Check for overlapping leave requests (excluding current leave)
                var hasOverlappingLeave = await _leaveRepo.HasOverlappingLeaveAsync(entity.DriverId, entity.StartDate, entity.EndDate);
                if (hasOverlappingLeave)
                {
                    // Get detailed information about overlapping leaves (excluding current leave)
                    var overlappingLeaves = await _leaveRepo.GetLeavesByDriverAndDateRangeAsync(entity.DriverId, entity.StartDate, entity.EndDate);
                    var activeOverlappingLeaves = overlappingLeaves
                        .Where(l => l.Id != leaveId && (l.Status == LeaveStatus.Pending || l.Status == LeaveStatus.Approved))
                        .ToList();
                    
                    if (activeOverlappingLeaves.Any())
                    {
                        var overlappingLeave = activeOverlappingLeaves.First();
                        var statusText = overlappingLeave.Status == LeaveStatus.Pending ? "pending" : "approved";
                        
                        _logger.LogWarning("Leave request update rejected: Overlapping leave exists. Driver: {DriverId}, " +
                            "Updated: {StartDate} to {EndDate}, Existing: {ExistingStartDate} to {ExistingEndDate}, Status: {Status}", 
                            entity.DriverId, entity.StartDate, entity.EndDate, overlappingLeave.StartDate, overlappingLeave.EndDate, overlappingLeave.Status);
                        
                        throw new InvalidOperationException(
                            $"You already have a {statusText} leave request from {overlappingLeave.StartDate:yyyy-MM-dd} to {overlappingLeave.EndDate:yyyy-MM-dd}. " +
                            "Please cancel or modify your existing request before updating this one.");
                    }
                }
            }
            
            entity.UpdatedAt = DateTime.UtcNow;
            var updated = await _leaveRepo.UpdateAsync(entity);
            return _mapper.Map<DriverLeaveResponse>(updated!);
        }

        public async Task<DriverLeaveResponse> CancelLeaveRequestAsync(Guid leaveId, Guid driverId)
        {
            var entity = await _leaveRepo.FindAsync(leaveId);
            if (entity == null || entity.IsDeleted || entity.DriverId != driverId) throw new InvalidOperationException("Leave not found");
            entity.Status = LeaveStatus.Cancelled;
            entity.UpdatedAt = DateTime.UtcNow;
            var updated = await _leaveRepo.UpdateAsync(entity);
            return _mapper.Map<DriverLeaveResponse>(updated!);
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
            
            // Lưu thông tin replacement driver nếu có
            if (dto.ReplacementDriverId.HasValue)
            {
                entity.SuggestedReplacementDriverId = dto.ReplacementDriverId.Value;
                entity.SuggestionGeneratedAt = DateTime.UtcNow;
            }
            
            // TODO: Commented out validation for EffectiveFrom/EffectiveTo as these properties are not being used effectively
            // The validation only checks if dates match but doesn't provide real value
            // Consider removing these properties entirely or implementing proper functionality
            /*
            if (dto.EffectiveFrom.HasValue && dto.EffectiveFrom.Value != entity.StartDate)
            {
                throw new InvalidOperationException("EffectiveFrom date must match the leave request start date.");
            }
            
            if (dto.EffectiveTo.HasValue && dto.EffectiveTo.Value != entity.EndDate)
            {
                throw new InvalidOperationException("EffectiveTo date must match the leave request end date.");
            }
            */
            
            //entity.Status = dto.IsApproved ? LeaveStatus.Approved : LeaveStatus.Rejected;
            entity.Status = LeaveStatus.Approved;
            entity.ApprovedAt = DateTime.UtcNow;
            entity.ApprovedByAdminId = adminId;
            entity.ApprovalNote = dto.Notes;
            var updated = await _leaveRepo.UpdateAsync(entity);
            
            // Send notification to driver about approval/rejection
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
                    ["leaveRequestId"] = entity.Id,
                    ["driverId"] = entity.DriverId,
                    ["adminId"] = adminId,
                    ["status"] = entity.Status.ToString(),
                    ["approvalNote"] = dto.Notes ?? "",
                    ["replacementDriverId"] = dto.ReplacementDriverId?.ToString() ?? ""
                };
                
                await _notificationService.CreateDriverLeaveNotificationAsync(
                    entity.Id, 
                    NotificationType.LeaveApproval, 
                    message, 
                    metadata
                );
                
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
            
            // TODO: Commented out validation for SuggestedAlternativeStartDate/SuggestedAlternativeEndDate
            // These properties are not being used effectively - validation exists but no storage or notification
            // Consider implementing proper functionality or removing these properties entirely
            /*
            if (dto.SuggestedAlternativeStartDate.HasValue && dto.SuggestedAlternativeEndDate.HasValue)
            {
                var today = DateTime.UtcNow.Date;
                
                if (dto.SuggestedAlternativeStartDate.Value < today)
                {
                    throw new InvalidOperationException("Suggested alternative start date cannot be in the past.");
                }
                
                if (dto.SuggestedAlternativeEndDate.Value < today)
                {
                    throw new InvalidOperationException("Suggested alternative end date cannot be in the past.");
                }
                
                if (dto.SuggestedAlternativeEndDate.Value < dto.SuggestedAlternativeStartDate.Value)
                {
                    throw new InvalidOperationException("Suggested alternative end date cannot be before suggested start date.");
                }
            }
            else if (dto.SuggestedAlternativeStartDate.HasValue || dto.SuggestedAlternativeEndDate.HasValue)
            {
                throw new InvalidOperationException("Both suggested alternative start date and end date must be provided together.");
            }
            */
            
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
                    ["leaveRequestId"] = entity.Id,
                    ["driverId"] = entity.DriverId,
                    ["adminId"] = adminId,
                    ["rejectionReason"] = dto.Reason
                };
                
                await _notificationService.CreateDriverLeaveNotificationAsync(
                    entity.Id, 
                    NotificationType.LeaveApproval, 
                    message, 
                    metadata
                );
                
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

        public Task<ReplacementSuggestionResponse> AcceptReplacementSuggestionAsync(Guid leaveId, Guid suggestionId, Guid adminId)
        {
            // For MVP, simply return success without persisting an acceptance entity
            return Task.FromResult(new ReplacementSuggestionResponse { Success = true, Message = "Accepted", TotalSuggestions = 0 });
        }

        public Task<ReplacementSuggestionResponse> RejectReplacementSuggestionAsync(Guid leaveId, Guid suggestionId, Guid adminId)
        {
            return Task.FromResult(new ReplacementSuggestionResponse { Success = true, Message = "Rejected", TotalSuggestions = 0 });
        }

        public async Task<IEnumerable<DriverLeaveConflictDto>> DetectConflictsAsync(Guid leaveId)
        {
            var conflicts = await _conflictRepo.GetByLeaveRequestIdAsync(leaveId);
            return conflicts.Select(_mapper.Map<DriverLeaveConflictDto>);
        }

        public Task<ConflictResolutionResponse> ResolveConflictsAsync(Guid leaveId, Guid adminId)
        {
            // MVP: return empty resolution
            return Task.FromResult(new ConflictResolutionResponse { Success = true, Message = "No conflicts to resolve", ResolvedAt = DateTime.UtcNow });
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

        public async Task<IEnumerable<DriverLeaveResponse>> GetPendingLeavesAsync()
        {
            var list = await _leaveRepo.GetPendingLeavesAsync();
            return list.Select(_mapper.Map<DriverLeaveResponse>);
        }

        public async Task<DriverLeaveResponse?> GetLeaveByIdAsync(Guid leaveId)
        {
            var entity = await _leaveRepo.FindAsync(leaveId);
            return entity == null ? null : _mapper.Map<DriverLeaveResponse>(entity);
        }

        public async Task<IEnumerable<DriverLeaveResponse>> GetLeavesByStatusAsync(LeaveStatus status)
        {
            var list = await _leaveRepo.GetLeavesByStatusAsync(status);
            return list.Select(l => _mapper.Map<DriverLeaveResponse>(l));
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
    }
}
