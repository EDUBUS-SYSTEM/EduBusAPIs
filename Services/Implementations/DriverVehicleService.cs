using AutoMapper;
using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using Services.Contracts;
using Services.Models.Driver;
using Services.Models.DriverVehicle;
using Services.Models.UserAccount;
using Utils;

namespace Services.Implementations
{
    public class DriverVehicleService : IDriverVehicleService
    {
        private readonly IDriverVehicleRepository _driverVehicleRepo;
        private readonly IVehicleRepository _vehicleRepo;
        private readonly IDriverRepository _driverRepo;
        private readonly IMapper _mapper;

        public DriverVehicleService(
            IDriverVehicleRepository driverVehicleRepo,
            IVehicleRepository vehicleRepo,
            IMapper mapper,
            IDriverRepository driverRepo)
        {
            _driverVehicleRepo = driverVehicleRepo;
            _vehicleRepo = vehicleRepo;
            _mapper = mapper;
            _driverRepo = driverRepo;
        }

        public async Task<VehicleDriversResponse?> GetDriversByVehicleAsync(Guid vehicleId, bool? isActive)
        {
            var vehicle = await _vehicleRepo.FindAsync(vehicleId);
            if (vehicle == null || vehicle.IsDeleted) return null;

            var assignments = await _driverVehicleRepo.GetByVehicleIdAsync(vehicleId, isActive);

            return new VehicleDriversResponse
            {
                Success = true,
                Data = assignments.Select(_mapper.Map<DriverAssignmentDto>).ToList()
            };
        }

        public async Task<IEnumerable<DriverInfoDto>> GetDriversNotAssignedToVehicleAsync(
        Guid vehicleId, DateTime start, DateTime end)
        {
            var drivers = await _driverVehicleRepo.GetDriversNotAssignedToVehicleAsync(vehicleId, start, end);

            return drivers.Select(d => new DriverInfoDto
            {
                Id = d.Id,
                FullName = $"{d.FirstName} {d.LastName}",
                Email = d.Email,
                PhoneNumber = d.PhoneNumber,
                Status = d.Status,
                LicenseNumber = d.DriverLicense != null
                    ? SecurityHelper.DecryptFromBytes(d.DriverLicense.HashedLicenseNumber)
                    : null,
                HasValidLicense = d.DriverLicense != null,
                HasHealthCertificate = d.HealthCertificateFileId.HasValue
            });
        }
        public async Task<DriverAssignmentResponse?> AssignDriverAsync(Guid vehicleId, DriverAssignmentRequest dto, Guid adminId)
        {
            var vehicle = await _vehicleRepo.FindAsync(vehicleId);
            if (vehicle == null || vehicle.IsDeleted) return null;

            if (dto.EndTimeUtc.HasValue && dto.EndTimeUtc <= dto.StartTimeUtc)
                throw new InvalidOperationException("End time cannot be earlier than start time.");

            var alreadyAssigned = await _driverVehicleRepo.IsDriverAlreadyAssignedAsync(vehicleId, dto.DriverId, true);
            if (alreadyAssigned)
                throw new InvalidOperationException("This driver is already assigned to the vehicle");

            var entity = new DriverVehicle
            {
                DriverId = dto.DriverId,
                VehicleId = vehicleId,
                IsPrimaryDriver = dto.IsPrimaryDriver,
                StartTimeUtc = dto.StartTimeUtc,
                EndTimeUtc = dto.EndTimeUtc,
                AssignedByAdminId = adminId
            };

            var created = await _driverVehicleRepo.AssignDriverAsync(entity);

            var dtoResult = _mapper.Map<DriverAssignmentDto>(created);
            dtoResult.AssignedByAdminId = adminId; 

            return new DriverAssignmentResponse
            {
                Success = true,
                Data = dtoResult
            };
        }

        public async Task<DriverAssignmentResponse?> AssignDriverWithValidationAsync(Guid vehicleId, DriverAssignmentRequest dto, Guid adminId)
        {
            // extra validations: overlapping windows
            var timeConflict = await _driverVehicleRepo.HasTimeConflictAsync(dto.DriverId, dto.StartTimeUtc, dto.EndTimeUtc);
            if (timeConflict) throw new InvalidOperationException("Driver has conflicting assignment in the selected time window.");
            var vehicleConflict = await _driverVehicleRepo.HasVehicleTimeConflictAsync(vehicleId, dto.StartTimeUtc, dto.EndTimeUtc);
            if (vehicleConflict) throw new InvalidOperationException("Vehicle is already assigned in the selected time window.");
            return await AssignDriverAsync(vehicleId, dto, adminId);
        }

        public async Task<DriverAssignmentResponse?> UpdateAssignmentAsync(Guid assignmentId, UpdateAssignmentRequest dto, Guid adminId)
        {
            var assignment = await _driverVehicleRepo.FindAsync(assignmentId);
            if (assignment == null || assignment.IsDeleted) return null;
            if (dto.IsPrimaryDriver.HasValue) assignment.IsPrimaryDriver = dto.IsPrimaryDriver.Value;
            if (dto.StartTimeUtc.HasValue && dto.EndTimeUtc.HasValue && dto.EndTimeUtc <= dto.StartTimeUtc)
                throw new InvalidOperationException("End time cannot be earlier than start time.");
            if (dto.EndTimeUtc.HasValue) assignment.EndTimeUtc = dto.EndTimeUtc.Value;
            if (!string.IsNullOrWhiteSpace(dto.AssignmentReason)) assignment.AssignmentReason = dto.AssignmentReason;
            assignment.UpdatedAt = DateTime.UtcNow;
            var updated = await _driverVehicleRepo.UpdateAsync(assignment);
            return new DriverAssignmentResponse { Success = true, Data = _mapper.Map<DriverAssignmentDto>(updated!) };
        }

        public async Task<DriverAssignmentResponse?> CancelAssignmentAsync(Guid assignmentId, string reason, Guid adminId)
        {
            var assignment = await _driverVehicleRepo.FindAsync(assignmentId);
            if (assignment == null || assignment.IsDeleted) return null;
            assignment.Status = DriverVehicleStatus.Cancelled;
            assignment.EndTimeUtc = assignment.EndTimeUtc ?? DateTime.UtcNow;
            assignment.ApprovalNote = reason;
            assignment.ApprovedByAdminId = adminId;         
            assignment.ApprovedAt = DateTime.UtcNow;       
            assignment.UpdatedAt = DateTime.UtcNow;         
            var updated = await _driverVehicleRepo.UpdateAsync(assignment);
            return new DriverAssignmentResponse { Success = true, Data = MapToDriverAssignmentDto(updated!) };
        }
        public async Task<BasicSuccessResponse?> DeleteAssignmentAsync(Guid assignmentId, Guid adminId)
        {
            var assignment = await _driverVehicleRepo.FindAsync(assignmentId);
            if (assignment == null || assignment.IsDeleted) return null;

            await _driverVehicleRepo.DeleteAsync(assignment);

            return new BasicSuccessResponse
            {
                Success = true,
                Data = new { Message = "Assignment deleted (soft)" }
            };
        }

        public async Task<IEnumerable<AssignmentConflictDto>> DetectAssignmentConflictsAsync(Guid vehicleId, DateTime startTime, DateTime endTime)
        {
            var conflicts = new List<AssignmentConflictDto>();
            var vehicleConflict = await _driverVehicleRepo.HasVehicleTimeConflictAsync(vehicleId, startTime, endTime);
            if (vehicleConflict)
            {
                conflicts.Add(new AssignmentConflictDto
                {
                    ConflictId = Guid.NewGuid(),
                    ConflictType = "VehicleTimeConflict",
                    Description = "Vehicle has overlapping assignment in this time window",
                    Severity = Data.Models.Enums.ConflictSeverity.Medium,
                    ConflictTime = DateTime.UtcNow,
                    IsResolvable = true
                });
            }
            return conflicts;
        }

        public async Task<ReplacementSuggestionResponse> SuggestReplacementAsync(Guid assignmentId, Guid adminId)
        {
            var assignment = await _driverVehicleRepo.FindAsync(assignmentId) ?? throw new InvalidOperationException("Assignment not found");
            var drivers = await _driverVehicleRepo.FindAvailableDriversAsync(assignment.StartTimeUtc, assignment.EndTimeUtc ?? assignment.StartTimeUtc.AddHours(4));
            var vehicles = await _driverVehicleRepo.FindAvailableVehiclesAsync(assignment.StartTimeUtc, assignment.EndTimeUtc ?? assignment.StartTimeUtc.AddHours(4));
            var suggestions = new List<ReplacementSuggestionDto>();
            foreach (var d in drivers.Take(3))
            {
                var v = vehicles.FirstOrDefault();
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
            return new ReplacementSuggestionResponse { Success = true, Suggestions = suggestions, TotalSuggestions = suggestions.Count, Message = "Suggestions generated" };
        }

        public Task<bool> AcceptReplacementSuggestionAsync(Guid assignmentId, Guid suggestionId, Guid adminId)
        {
            // MVP: acknowledge only
            return Task.FromResult(true);
        }

        public async Task<DriverAssignmentResponse?> ApproveAssignmentAsync(Guid assignmentId, Guid adminId, string? note)
        {
            var assignment = await _driverVehicleRepo.FindAsync(assignmentId);
            if (assignment == null) return null;
            assignment.Status = DriverVehicleStatus.Active;
            assignment.ApprovedByAdminId = adminId;
            assignment.ApprovedAt = DateTime.UtcNow;
            assignment.ApprovalNote = note;
            var updated = await _driverVehicleRepo.UpdateAsync(assignment);
            return new DriverAssignmentResponse { Success = true, Data = MapToDriverAssignmentDto(updated!) };
        }

        public async Task<DriverAssignmentResponse?> RejectAssignmentAsync(Guid assignmentId, Guid adminId, string reason)
        {
            var assignment = await _driverVehicleRepo.FindAsync(assignmentId);
            if (assignment == null) return null;
            assignment.Status = DriverVehicleStatus.Cancelled;
            assignment.ApprovedByAdminId = adminId;
            assignment.ApprovedAt = DateTime.UtcNow;
            assignment.ApprovalNote = $"Rejected: {reason}";
            var updated = await _driverVehicleRepo.UpdateAsync(assignment);
            return new DriverAssignmentResponse { Success = true, Data = _mapper.Map<DriverAssignmentDto>(updated!) };
        }

        public async Task<AssignmentListResponse> GetAssignmentsWithFiltersAsync(AssignmentListRequest request)
        {
            try
            {
                var result = await _driverVehicleRepo.GetAssignmentsWithFiltersAsync(
                    request.DriverId,
                    request.VehicleId,
                    request.Status.HasValue ? (int)request.Status.Value : null,
                    request.IsPrimaryDriver,
                    request.StartDateFrom,
                    request.StartDateTo,
                    request.EndDateFrom,
                    request.EndDateTo,
                    request.AssignedByAdminId,
                    request.ApprovedByAdminId,
                    request.IsActive,
                    request.IsUpcoming,
                    request.IsCompleted,
                    request.SearchTerm,
                    request.Page,
                    request.PerPage,
                    request.SortBy,
                    request.SortOrder);

                var assignments = result.assignments;
                var totalCount = result.totalCount;

                var assignmentDtos = assignments.Select(MapToDriverAssignmentDto).ToList();

                var totalPages = (int)Math.Ceiling((double)totalCount / request.PerPage);

                // Create filter summary manually
                var filterSummary = new FilterSummary
                {
                    TotalAssignments = totalCount,
                    ActiveAssignments = assignments.Count(a => a.StartTimeUtc <= DateTime.UtcNow && (!a.EndTimeUtc.HasValue || a.EndTimeUtc > DateTime.UtcNow)),
                    PendingAssignments = assignments.Count(a => a.Status == DriverVehicleStatus.Pending),
                    CompletedAssignments = assignments.Count(a => a.Status == DriverVehicleStatus.Completed),
                    CancelledAssignments = assignments.Count(a => a.Status == DriverVehicleStatus.Cancelled),
                    SuspendedAssignments = assignments.Count(a => a.Status == DriverVehicleStatus.Suspended),
                    UpcomingAssignments = assignments.Count(a => a.StartTimeUtc > DateTime.UtcNow),
                    EarliestStartDate = assignments.Any() ? assignments.Min(a => a.StartTimeUtc) : null,
                    LatestEndDate = assignments.Where(a => a.EndTimeUtc.HasValue).Any() ? assignments.Where(a => a.EndTimeUtc.HasValue).Max(a => a.EndTimeUtc) : null
                };

                return new AssignmentListResponse
                {
                    Success = true,
                    Data = assignmentDtos,
                    Pagination = new PaginationInfo
                    {
                        CurrentPage = request.Page,
                        PerPage = request.PerPage,
                        TotalItems = totalCount,
                        TotalPages = totalPages,
                        HasNextPage = request.Page < totalPages,
                        HasPreviousPage = request.Page > 1
                    },
                    Filters = filterSummary
                };
            }
            catch (Exception ex)
            {
                return new AssignmentListResponse
                {
                    Success = false,
                    Error = new { message = "An error occurred while retrieving assignments.", details = ex.Message }
                };
            }
        }

        public async Task<DriverAssignmentSummaryResponse> GetDriverAssignmentSummaryAsync(Guid driverId)
        {
            try
            {
                var assignments = await _driverVehicleRepo.GetDriverAssignmentsAsync(driverId);
                var assignmentList = assignments.ToList();
                var now = DateTime.UtcNow;

                var driver = await _driverRepo.FindAsync(driverId);
                if (driver == null)
                {
                    return new DriverAssignmentSummaryResponse
                    {
                        Success = false,
                        Error = new { message = "Driver not found." }
                    };
                }

                var currentAssignments = assignmentList.Where(a => a.StartTimeUtc <= now && (!a.EndTimeUtc.HasValue || a.EndTimeUtc > now)).ToList();
                var completedAssignments = assignmentList.Where(a => a.EndTimeUtc.HasValue && a.EndTimeUtc <= now).ToList();
                var upcomingAssignments = assignmentList.Where(a => a.StartTimeUtc > now).ToList();

                var totalWorkingHours = TimeSpan.FromHours(completedAssignments.Sum(a => 
                    ((a.EndTimeUtc ?? a.StartTimeUtc.AddHours(4)) - a.StartTimeUtc).TotalHours));

                var summary = new DriverAssignmentSummaryDto
                {
                    DriverId = driverId,
                    DriverName = $"{driver.FirstName} {driver.LastName}",
                    DriverEmail = driver.Email,
                    DriverStatus = driver.Status,
                    CurrentAssignments = currentAssignments.Select(MapToDriverAssignmentDto).ToList(),
                    TotalCurrentAssignments = currentAssignments.Count,
                    HasActiveAssignments = currentAssignments.Any(),
                    TotalAssignments = assignmentList.Count,
                    CompletedAssignments = completedAssignments.Count,
                    CancelledAssignments = assignmentList.Count(a => a.Status == DriverVehicleStatus.Cancelled),
                    PendingAssignments = assignmentList.Count(a => a.Status == DriverVehicleStatus.Pending),
                    TotalWorkingHours = totalWorkingHours,
                    LastAssignmentDate = assignmentList.OrderByDescending(a => a.StartTimeUtc).FirstOrDefault()?.StartTimeUtc,
                    NextAssignmentDate = upcomingAssignments.OrderBy(a => a.StartTimeUtc).FirstOrDefault()?.StartTimeUtc,
                    AssignedVehicleIds = assignmentList.Select(a => a.VehicleId).Distinct().ToList(),
                    TotalVehiclesAssigned = assignmentList.Select(a => a.VehicleId).Distinct().Count(),
                    AssignmentCompletionRate = assignmentList.Count > 0 ? (double)completedAssignments.Count / assignmentList.Count * 100 : 0,
                    OnTimeAssignments = completedAssignments.Count, // Placeholder - would need actual trip data
                    LateAssignments = 0, // Placeholder - would need actual trip data
                    PunctualityRate = 100 // Placeholder - would need actual trip data
                };

                return new DriverAssignmentSummaryResponse
                {
                    Success = true,
                    Data = summary
                };
            }
            catch (Exception ex)
            {
                return new DriverAssignmentSummaryResponse
                {
                    Success = false,
                    Error = new { message = "An error occurred while retrieving driver assignment summary.", details = ex.Message }
                };
            }
        }

        public async Task<AssignmentListResponse> GetDriverAssignmentsAsync(Guid driverId, bool? isActive = null, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int perPage = 20)
        {
            try
            {
                var assignments = await _driverVehicleRepo.GetDriverAssignmentsAsync(driverId, isActive, startDate, endDate);
                var assignmentList = assignments.ToList();

                var totalCount = assignmentList.Count;
                var totalPages = (int)Math.Ceiling((double)totalCount / perPage);

                var pagedAssignments = assignmentList
                    .Skip((page - 1) * perPage)
                    .Take(perPage)
                    .Select(MapToDriverAssignmentDto)
                    .ToList();

                return new AssignmentListResponse
                {
                    Success = true,
                    Data = pagedAssignments,
                    Pagination = new PaginationInfo
                    {
                        CurrentPage = page,
                        PerPage = perPage,
                        TotalItems = totalCount,
                        TotalPages = totalPages,
                        HasNextPage = page < totalPages,
                        HasPreviousPage = page > 1
                    }
                };
            }
            catch (Exception ex)
            {
                return new AssignmentListResponse
                {
                    Success = false,
                    Error = new { message = "An error occurred while retrieving driver assignments.", details = ex.Message }
                };
            }
        }

        public async Task<DriverAssignmentResponse?> UpdateAssignmentStatusAsync(Guid assignmentId, DriverVehicleStatus status, Guid adminId, string? note = null)
        {
            var assignment = await _driverVehicleRepo.FindAsync(assignmentId);
            if (assignment == null) return null;

            assignment.Status = status;
            assignment.UpdatedAt = DateTime.UtcNow;

            if (status == DriverVehicleStatus.Active || status == DriverVehicleStatus.Cancelled)
            {
                assignment.ApprovedByAdminId = adminId;
                assignment.ApprovedAt = DateTime.UtcNow;
                assignment.ApprovalNote = note;
            }

            var updated = await _driverVehicleRepo.UpdateAsync(assignment);
            return new DriverAssignmentResponse { Success = true, Data = MapToDriverAssignmentDto(updated!) };
        }

        private DriverAssignmentDto MapToDriverAssignmentDto(DriverVehicle assignment)
        {
            var now = DateTime.UtcNow;
            var dto = _mapper.Map<DriverAssignmentDto>(assignment);
            
            // Set computed properties
            dto.IsActive = assignment.StartTimeUtc <= now && (!assignment.EndTimeUtc.HasValue || assignment.EndTimeUtc > now);
            dto.IsUpcoming = assignment.StartTimeUtc > now;
            dto.IsCompleted = assignment.EndTimeUtc.HasValue && assignment.EndTimeUtc <= now;
            dto.Duration = assignment.EndTimeUtc.HasValue ? assignment.EndTimeUtc - assignment.StartTimeUtc : null;

            // Map navigation properties
            if (assignment.Driver != null)
            {
                dto.Driver = new DriverInfoDto
                {
                    Id = assignment.Driver.Id,
                    FullName = $"{assignment.Driver.FirstName} {assignment.Driver.LastName}",
                    Email = assignment.Driver.Email,
                    PhoneNumber = assignment.Driver.PhoneNumber,
                    Status = assignment.Driver.Status,
                    LicenseNumber = assignment.Driver.DriverLicense != null ? SecurityHelper.DecryptFromBytes(assignment.Driver.DriverLicense.HashedLicenseNumber) : null,
                    LicenseExpiryDate = null, // DriverLicense doesn't have expiry date in current model
                    HasValidLicense = assignment.Driver.DriverLicense != null,
                    HasHealthCertificate = assignment.Driver.HealthCertificateFileId.HasValue
                };
            }

            if (assignment.Vehicle != null)
            {
                dto.Vehicle = new VehicleInfoDto
                {
                    Id = assignment.Vehicle.Id,
                    LicensePlate = SecurityHelper.DecryptFromBytes(assignment.Vehicle.HashedLicensePlate),
                    VehicleType = "Bus", // Default type since Vehicle model doesn't have VehicleType
                    Capacity = assignment.Vehicle.Capacity,
                    Status = assignment.Vehicle.Status.ToString(),
                    Description = assignment.Vehicle.StatusNote // Use StatusNote as description
                };
            }

            if (assignment.AssignedByAdmin != null)
            {
                dto.AssignedByAdmin = new AdminInfoDto
                {
                    Id = assignment.AssignedByAdmin.Id,
                    FullName = $"{assignment.AssignedByAdmin.FirstName} {assignment.AssignedByAdmin.LastName}",
                    Email = assignment.AssignedByAdmin.Email
                };
            }

            if (assignment.ApprovedByAdmin != null)
            {
                dto.ApprovedByAdmin = new AdminInfoDto
                {
                    Id = assignment.ApprovedByAdmin.Id,
                    FullName = $"{assignment.ApprovedByAdmin.FirstName} {assignment.ApprovedByAdmin.LastName}",
                    Email = assignment.ApprovedByAdmin.Email
                };
            }

            return dto;
        }
    }
}
