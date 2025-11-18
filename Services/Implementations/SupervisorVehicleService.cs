using AutoMapper;
using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using Services.Contracts;
using Services.Models.SupervisorVehicle;
using Services.Models.Common;
using Services.Models.UserAccount;
using Utils;
using Microsoft.EntityFrameworkCore;

namespace Services.Implementations
{
    public class SupervisorVehicleService : ISupervisorVehicleService
    {
        private readonly ISupervisorVehicleRepository _supervisorVehicleRepo;
        private readonly IVehicleRepository _vehicleRepo;
        private readonly ISupervisorRepository _supervisorRepo;
        private readonly IMapper _mapper;

        public SupervisorVehicleService(
            ISupervisorVehicleRepository supervisorVehicleRepo,
            IVehicleRepository vehicleRepo,
            ISupervisorRepository supervisorRepo,
            IMapper mapper)
        {
            _supervisorVehicleRepo = supervisorVehicleRepo;
            _vehicleRepo = vehicleRepo;
            _supervisorRepo = supervisorRepo;
            _mapper = mapper;
        }

        public async Task<VehicleSupervisorsResponse?> GetSupervisorsByVehicleAsync(Guid vehicleId, bool? isActive)
        {
            var vehicle = await _vehicleRepo.FindAsync(vehicleId);
            if (vehicle == null || vehicle.IsDeleted) return null;

            var assignments = await _supervisorVehicleRepo.GetByVehicleIdAsync(vehicleId, isActive);

            var dtos = _mapper.Map<List<SupervisorAssignmentDto>>(assignments.Where(a => !a.IsDeleted));

            return new VehicleSupervisorsResponse
            {
                Success = true,
                Data = dtos
            };
        }

        public async Task<SupervisorAssignmentResponse?> AssignSupervisorAsync(Guid vehicleId, SupervisorAssignmentRequest dto, Guid adminId)
        {
            var vehicle = await _vehicleRepo.FindAsync(vehicleId);
            if (vehicle == null || vehicle.IsDeleted) return null;

            var supervisor = await _supervisorRepo.FindAsync(dto.SupervisorId);
            if (supervisor == null || supervisor.IsDeleted) return null;

            // Validate start date is not in the past
            if (dto.StartTimeUtc.Date < DateTime.UtcNow.Date)
                throw new InvalidOperationException("Start date cannot be in the past.");

            if (dto.EndTimeUtc.HasValue && dto.EndTimeUtc <= dto.StartTimeUtc)
                throw new InvalidOperationException("End time cannot be earlier than start time.");

            // Ensure only 1 supervisor per vehicle at a time - check for vehicle conflict
            var vehicleConflict = await _supervisorVehicleRepo.HasVehicleTimeConflictAsync(vehicleId, dto.StartTimeUtc, dto.EndTimeUtc);
            if (vehicleConflict)
                throw new InvalidOperationException("Vehicle already has a supervisor assigned in the selected time window. Only one supervisor can be assigned to a vehicle at a time.");

            var entity = new SupervisorVehicle
            {
                SupervisorId = dto.SupervisorId,
                VehicleId = vehicleId,
                StartTimeUtc = dto.StartTimeUtc,
                EndTimeUtc = dto.EndTimeUtc,
                Status = SupervisorVehicleStatus.Assigned,
                AssignmentReason = dto.AssignmentReason,
                AssignedByAdminId = adminId
            };

            var created = await _supervisorVehicleRepo.AssignSupervisorAsync(entity);

            return new SupervisorAssignmentResponse
            {
                Success = true,
                Data = _mapper.Map<SupervisorAssignmentDto>(created)
            };
        }

        public async Task<SupervisorAssignmentResponse?> AssignSupervisorWithValidationAsync(Guid vehicleId, SupervisorAssignmentRequest dto, Guid adminId)
        {
            // Check time conflict with other supervisors
            var timeConflict = await _supervisorVehicleRepo.HasTimeConflictAsync(dto.SupervisorId, dto.StartTimeUtc, dto.EndTimeUtc);
            if (timeConflict)
                throw new InvalidOperationException("Supervisor has conflicting assignment in the selected time window.");

            // Check vehicle conflict
            var vehicleConflict = await _supervisorVehicleRepo.HasVehicleTimeConflictAsync(vehicleId, dto.StartTimeUtc, dto.EndTimeUtc);
            if (vehicleConflict)
                throw new InvalidOperationException("Vehicle already has a supervisor assigned in the selected time window.");

            return await AssignSupervisorAsync(vehicleId, dto, adminId);
        }

        public async Task<SupervisorAssignmentResponse?> UpdateAssignmentAsync(Guid assignmentId, UpdateSupervisorAssignmentRequest dto, Guid adminId)
        {
            var assignment = await _supervisorVehicleRepo.FindAsync(assignmentId);
            if (assignment == null || assignment.IsDeleted) return null;

            // Determine new time range (use existing if not provided in dto)
            var newStartTime = dto.StartTimeUtc ?? assignment.StartTimeUtc;
            var newEndTime = dto.EndTimeUtc.HasValue ? dto.EndTimeUtc : assignment.EndTimeUtc;

            // Validate start date is not in the past
            if (newStartTime.Date < DateTime.UtcNow.Date)
                throw new InvalidOperationException("Start date cannot be in the past.");

            // Validate end time > start time
            if (newEndTime.HasValue && newEndTime <= newStartTime)
                throw new InvalidOperationException("End time cannot be earlier than start time.");

            // Check conflict with OTHER assignments of the SAME SUPERVISOR (exclude current assignment)
            var supervisorConflict = await _supervisorVehicleRepo.HasTimeConflictAsync(
                assignment.SupervisorId,
                newStartTime,
                newEndTime,
                assignmentId
            );
            if (supervisorConflict)
                throw new InvalidOperationException("Supervisor has conflicting assignment with another vehicle in the selected time window.");

            // Check conflict with OTHER assignments of the SAME VEHICLE (exclude current assignment)
            var vehicleConflict = await _supervisorVehicleRepo.HasVehicleTimeConflictAsync(
                assignment.VehicleId,
                newStartTime,
                newEndTime,
                assignmentId
            );
            if (vehicleConflict)
                throw new InvalidOperationException("Vehicle has conflicting assignment with another supervisor in the selected time window.");

            // Update fields
            if (dto.StartTimeUtc.HasValue) assignment.StartTimeUtc = dto.StartTimeUtc.Value;
            if (dto.EndTimeUtc.HasValue) assignment.EndTimeUtc = dto.EndTimeUtc.Value;
            if (!string.IsNullOrWhiteSpace(dto.AssignmentReason)) assignment.AssignmentReason = dto.AssignmentReason;
            assignment.UpdatedAt = DateTime.UtcNow;

            var updated = await _supervisorVehicleRepo.UpdateAsync(assignment);

            return new SupervisorAssignmentResponse { Success = true, Data = _mapper.Map<SupervisorAssignmentDto>(updated) };
        }

        public async Task<SupervisorAssignmentResponse?> CancelAssignmentAsync(Guid assignmentId, string reason, Guid adminId)
        {
            var assignment = await _supervisorVehicleRepo.FindAsync(assignmentId);
            if (assignment == null || assignment.IsDeleted) return null;

            assignment.Status = SupervisorVehicleStatus.Unassigned;
            assignment.EndTimeUtc = assignment.EndTimeUtc ?? DateTime.UtcNow;
            assignment.ApprovalNote = reason;
            assignment.ApprovedByAdminId = adminId;
            assignment.ApprovedAt = DateTime.UtcNow;
            assignment.UpdatedAt = DateTime.UtcNow;
            assignment.IsDeleted = true;

            var updated = await _supervisorVehicleRepo.UpdateAsync(assignment);
            return new SupervisorAssignmentResponse { Success = true, Data = _mapper.Map<SupervisorAssignmentDto>(updated) };
        }

        public async Task<BasicSuccessResponse?> DeleteAssignmentAsync(Guid assignmentId, Guid adminId)
        {
            var assignment = await _supervisorVehicleRepo.FindAsync(assignmentId);
            if (assignment == null || assignment.IsDeleted) return null;

            await _supervisorVehicleRepo.DeleteAsync(assignment);

            return new BasicSuccessResponse
            {
                Success = true,
                Data = new { Message = "Assignment deleted (soft)" }
            };
        }

        public async Task<SupervisorAssignmentListResponse> GetSupervisorAssignmentsAsync(
            Guid supervisorId, 
            bool? isActive, 
            DateTime? startDate, 
            DateTime? endDate, 
            int page, 
            int perPage)
        {
            var supervisor = await _supervisorRepo.FindAsync(supervisorId);
            if (supervisor == null || supervisor.IsDeleted)
            {
                return new SupervisorAssignmentListResponse
                {
                    Success = false,
                    Error = "SUPERVISOR_NOT_FOUND"
                };
            }

            var allAssignments = (await _supervisorVehicleRepo.GetBySupervisorIdAsync(supervisorId))
                .Where(a => !a.IsDeleted)
                .ToList();
            
            // Apply filters
            var filtered = allAssignments.AsQueryable();
            
            if (isActive.HasValue)
            {
                var now = DateTime.UtcNow;
                if (isActive.Value)
                {
                    filtered = filtered.Where(a => a.StartTimeUtc <= now && (a.EndTimeUtc == null || a.EndTimeUtc > now));
                }
                else
                {
                    filtered = filtered.Where(a => a.StartTimeUtc > now || (a.EndTimeUtc.HasValue && a.EndTimeUtc <= now));
                }
            }

            if (startDate.HasValue)
            {
                filtered = filtered.Where(a => a.StartTimeUtc >= startDate.Value || (a.EndTimeUtc.HasValue && a.EndTimeUtc >= startDate.Value));
            }

            if (endDate.HasValue)
            {
                filtered = filtered.Where(a => a.StartTimeUtc <= endDate.Value);
            }

            // Pagination
            var totalItems = filtered.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)perPage);
            var skip = (page - 1) * perPage;

            var assignments = filtered
                .OrderByDescending(a => a.StartTimeUtc)
                .Skip(skip)
                .Take(perPage)
                .ToList();

            var dtos = _mapper.Map<List<SupervisorAssignmentDto>>(assignments);

            return new SupervisorAssignmentListResponse
            {
                Success = true,
                Data = dtos,
                Pagination = new PaginationInfo
                {
                    CurrentPage = page,
                    PerPage = perPage,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    HasNextPage = page < totalPages,
                    HasPreviousPage = page > 1
                }
            };
        }

        public async Task<SupervisorAssignmentDto?> GetSupervisorCurrentVehicleAsync(Guid supervisorId)
        {
            var now = DateTime.UtcNow;
            var assignments = await _supervisorVehicleRepo.GetActiveAssignmentsBySupervisorAsync(supervisorId);
            
            var currentAssignment = assignments
                .Where(a => a.StartTimeUtc <= now && (a.EndTimeUtc == null || a.EndTimeUtc > now))
                .OrderByDescending(a => a.StartTimeUtc)
                .FirstOrDefault();

            if (currentAssignment == null)
                return null;

            return _mapper.Map<SupervisorAssignmentDto>(currentAssignment);
        }
    }
}
