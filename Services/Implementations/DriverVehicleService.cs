using AutoMapper;
using Data.Models;
using Data.Repos.Interfaces;
using Services.Contracts;
using Services.Models.Driver;
using Services.Models.DriverVehicle;
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
            if (dto.StartTimeUtc.HasValue) assignment.StartTimeUtc = dto.StartTimeUtc.Value;
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
            assignment.EndTimeUtc = assignment.EndTimeUtc ?? DateTime.UtcNow;
            assignment.ApprovalNote = reason;
            var updated = await _driverVehicleRepo.UpdateAsync(assignment);
            return new DriverAssignmentResponse { Success = true, Data = _mapper.Map<DriverAssignmentDto>(updated!) };
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
            assignment.ApprovedByAdminId = adminId;
            assignment.ApprovedAt = DateTime.UtcNow;
            assignment.ApprovalNote = note;
            var updated = await _driverVehicleRepo.UpdateAsync(assignment);
            return new DriverAssignmentResponse { Success = true, Data = _mapper.Map<DriverAssignmentDto>(updated!) };
        }

        public async Task<DriverAssignmentResponse?> RejectAssignmentAsync(Guid assignmentId, Guid adminId, string reason)
        {
            var assignment = await _driverVehicleRepo.FindAsync(assignmentId);
            if (assignment == null) return null;
            assignment.ApprovedByAdminId = adminId;
            assignment.ApprovedAt = DateTime.UtcNow;
            assignment.ApprovalNote = $"Rejected: {reason}";
            var updated = await _driverVehicleRepo.UpdateAsync(assignment);
            return new DriverAssignmentResponse { Success = true, Data = _mapper.Map<DriverAssignmentDto>(updated!) };
        }
    }
}
