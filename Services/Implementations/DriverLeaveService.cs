using AutoMapper;
using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using Services.Contracts;
using Services.Models.Driver;
using Microsoft.Extensions.Logging;

namespace Services.Implementations
{
    public class DriverLeaveService : IDriverLeaveService
    {
        private readonly IDriverLeaveRepository _leaveRepo;
        private readonly IDriverLeaveConflictRepository _conflictRepo;
        private readonly IDriverRepository _driverRepo;
        private readonly IDriverVehicleRepository _driverVehicleRepo;
        private readonly INotificationService _notificationService;
        private readonly IMapper _mapper;
        private readonly ILogger<DriverLeaveService> _logger;

        public DriverLeaveService(
            IDriverLeaveRepository leaveRepo,
            IDriverLeaveConflictRepository conflictRepo,
            IDriverRepository driverRepo,
            IDriverVehicleRepository driverVehicleRepo,
            INotificationService notificationService,
            IMapper mapper,
            ILogger<DriverLeaveService> logger)
        {
            _leaveRepo = leaveRepo;
            _conflictRepo = conflictRepo;
            _driverRepo = driverRepo;
            _driverVehicleRepo = driverVehicleRepo;
            _notificationService = notificationService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<DriverLeaveResponse> CreateLeaveRequestAsync(CreateLeaveRequestDto dto)
        {
            var today = DateTime.Today;
            if (dto.StartDate < today)
            {
                throw new InvalidOperationException("Leave start date cannot be in the past.");
            }
            
            if (dto.EndDate < today)
            {
                throw new InvalidOperationException("Leave end date cannot be in the past.");
            }
            
            if (dto.EndDate < dto.StartDate)
            {
                throw new InvalidOperationException("Leave end date cannot be before start date.");
            }
            
            var entity = _mapper.Map<DriverLeaveRequest>(dto);
            entity.Id = Guid.NewGuid();
            entity.RequestedAt = DateTime.UtcNow;
            entity.Status = LeaveStatus.Pending;
            var created = await _leaveRepo.AddAsync(entity);
            
            // Send notification to admin about new leave request
            try
            {
                var driver = await _driverRepo.FindAsync(dto.DriverId);
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
            if (dto.LeaveType.HasValue) entity.LeaveType = dto.LeaveType.Value;
            if (dto.StartDate.HasValue) entity.StartDate = dto.StartDate.Value;
            if (dto.EndDate.HasValue) entity.EndDate = dto.EndDate.Value;
            if (!string.IsNullOrWhiteSpace(dto.Reason)) entity.Reason = dto.Reason;
            if (dto.AutoReplacementEnabled.HasValue) entity.AutoReplacementEnabled = dto.AutoReplacementEnabled.Value;
            if (!string.IsNullOrWhiteSpace(dto.AdditionalInformation)) { /* store if later modeled */ }
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
            
            if (dto.EffectiveFrom.HasValue && dto.EffectiveFrom.Value != entity.StartDate)
            {
                throw new InvalidOperationException("EffectiveFrom date must match the leave request start date.");
            }
            
            if (dto.EffectiveTo.HasValue && dto.EffectiveTo.Value != entity.EndDate)
            {
                throw new InvalidOperationException("EffectiveTo date must match the leave request end date.");
            }
            
            entity.Status = dto.IsApproved ? LeaveStatus.Approved : LeaveStatus.Rejected;
            entity.ApprovedAt = DateTime.UtcNow;
            entity.ApprovedByAdminId = adminId;
            entity.ApprovalNote = dto.Note;
            var updated = await _leaveRepo.UpdateAsync(entity);
            
            // Send notification to driver about approval/rejection
            try
            {
                var driver = await _driverRepo.FindAsync(entity.DriverId);
                var statusText = dto.IsApproved ? "approved" : "rejected";
                var message = $"Your leave request for {entity.LeaveType} from {entity.StartDate:yyyy-MM-dd} to {entity.EndDate:yyyy-MM-dd} has been {statusText}.";
                
                if (!string.IsNullOrEmpty(dto.Note))
                {
                    message += $" Note: {dto.Note}";
                }
                
                var metadata = new Dictionary<string, object>
                {
                    ["leaveRequestId"] = entity.Id,
                    ["driverId"] = entity.DriverId,
                    ["adminId"] = adminId,
                    ["status"] = entity.Status.ToString(),
                    ["approvalNote"] = dto.Note ?? ""
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
            
            if (dto.SuggestedAlternativeStartDate.HasValue && dto.SuggestedAlternativeEndDate.HasValue)
            {
                var today = DateTime.Today;
                
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
                    VehiclePlate = Utils.SecurityHelper.DecryptFromBytes(v.HashedLicensePlate),
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
            return list.Select(_mapper.Map<DriverLeaveResponse>);
        }
    }
}
