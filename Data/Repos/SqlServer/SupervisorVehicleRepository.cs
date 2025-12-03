using Data.Contexts.SqlServer;
using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Data.Repos.SqlServer
{
    public class SupervisorVehicleRepository : SqlRepository<SupervisorVehicle>, ISupervisorVehicleRepository
    {
        private readonly EduBusSqlContext _context;
        public SupervisorVehicleRepository(EduBusSqlContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SupervisorVehicle>> GetByVehicleIdAsync(Guid vehicleId, bool? isActive)
        {
            IQueryable<SupervisorVehicle> query = _context.SupervisorVehicles
                .Where(sv => sv.VehicleId == vehicleId && !sv.IsDeleted)
                .Include(sv => sv.Supervisor)
                .Include(sv => sv.Vehicle)
                .Include(sv => sv.AssignedByAdmin)
                .Include(sv => sv.ApprovedByAdmin);

            if (isActive.HasValue && isActive.Value)
            {
                var now = DateTime.UtcNow;
                query = query.Where(sv => sv.StartTimeUtc <= now && (sv.EndTimeUtc == null || sv.EndTimeUtc > now));
            }

            return await query.ToListAsync();
        }

        public async Task<SupervisorVehicle?> GetAssignmentAsync(Guid vehicleId, Guid supervisorId)
        {
            return await _context.SupervisorVehicles
                .Where(sv => sv.VehicleId == vehicleId && sv.SupervisorId == supervisorId && !sv.IsDeleted)
                .Include(sv => sv.Supervisor)
                .Include(sv => sv.Vehicle)
                .FirstOrDefaultAsync();
        }

        public async Task<SupervisorVehicle> AssignSupervisorAsync(SupervisorVehicle entity)
        {
            _context.SupervisorVehicles.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> IsSupervisorAlreadyAssignedAsync(Guid vehicleId, Guid supervisorId, bool onlyActive = true)
        {
            var query = _context.SupervisorVehicles
                .Where(sv => sv.VehicleId == vehicleId && sv.SupervisorId == supervisorId && !sv.IsDeleted);

            if (onlyActive)
            {
                var now = DateTime.UtcNow;
                query = query.Where(sv => sv.StartTimeUtc <= now && (sv.EndTimeUtc == null || sv.EndTimeUtc > now));
            }

            return await query.AnyAsync();
        }

        public async Task<bool> HasTimeConflictAsync(Guid supervisorId, DateTime startTime, DateTime? endTime, Guid? excludeAssignmentId = null)
        {
            var query = _context.SupervisorVehicles
                .Where(sv => sv.SupervisorId == supervisorId && !sv.IsDeleted);

            if (excludeAssignmentId.HasValue)
                query = query.Where(sv => sv.Id != excludeAssignmentId.Value);

            // Check if new time range overlaps with existing assignments
            var conflicts = await query
                .Where(sv => sv.StartTimeUtc < (endTime ?? DateTime.MaxValue) && (sv.EndTimeUtc == null || sv.EndTimeUtc > startTime))
                .AnyAsync();

            return conflicts;
        }

        public async Task<bool> HasVehicleTimeConflictAsync(Guid vehicleId, DateTime startTime, DateTime? endTime, Guid? excludeAssignmentId = null)
        {
            var query = _context.SupervisorVehicles
                .Where(sv => sv.VehicleId == vehicleId && !sv.IsDeleted);

            if (excludeAssignmentId.HasValue)
                query = query.Where(sv => sv.Id != excludeAssignmentId.Value);

            var conflicts = await query
                .Where(sv => sv.StartTimeUtc < (endTime ?? DateTime.MaxValue) && (sv.EndTimeUtc == null || sv.EndTimeUtc > startTime))
                .AnyAsync();

            return conflicts;
        }

        public async Task<IEnumerable<SupervisorVehicle>> GetActiveAssignmentsBySupervisorAsync(Guid supervisorId)
        {
            var now = DateTime.UtcNow;
            return await _context.SupervisorVehicles
                .Where(sv => sv.SupervisorId == supervisorId && !sv.IsDeleted &&
                             sv.StartTimeUtc <= now && (sv.EndTimeUtc == null || sv.EndTimeUtc > now))
                .Include(sv => sv.Supervisor)
                .Include(sv => sv.Vehicle)
                .ToListAsync();
        }

        public async Task<IEnumerable<SupervisorVehicle>> GetActiveAssignmentsByVehicleAsync(Guid vehicleId)
        {
            var now = DateTime.UtcNow;
            return await _context.SupervisorVehicles
                .Where(sv => sv.VehicleId == vehicleId && !sv.IsDeleted &&
                             sv.StartTimeUtc <= now && (sv.EndTimeUtc == null || sv.EndTimeUtc > now))
                .Include(sv => sv.Supervisor)
                .Include(sv => sv.Vehicle)
                .ToListAsync();
        }

        public async Task<SupervisorVehicle?> GetActiveSupervisorVehicleForVehicleByDateAsync(Guid vehicleId, DateTime serviceDate)
        {
            var startOfDay = serviceDate.Date;
            var endOfDay = startOfDay.AddDays(1);

            return await _context.SupervisorVehicles
                .Where(sv => sv.VehicleId == vehicleId && !sv.IsDeleted &&
                             sv.StartTimeUtc.Date <= serviceDate.Date &&
                             (sv.EndTimeUtc == null || sv.EndTimeUtc.Value.Date >= serviceDate.Date))
                .Include(sv => sv.Supervisor)
                .Include(sv => sv.Vehicle)
                .FirstOrDefaultAsync();
        }

        public async Task<SupervisorVehicle?> GetActiveSupervisorForVehicleAsync(Guid vehicleId)
        {
            var now = DateTime.UtcNow;
            return await _context.SupervisorVehicles
                .Where(sv => sv.VehicleId == vehicleId && !sv.IsDeleted &&
                             sv.StartTimeUtc <= now && (sv.EndTimeUtc == null || sv.EndTimeUtc > now))
                .Include(sv => sv.Supervisor)
                .Include(sv => sv.Vehicle)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<SupervisorVehicle>> GetBySupervisorIdAsync(Guid supervisorId)
        {
            return await _context.SupervisorVehicles
                .Where(sv => sv.SupervisorId == supervisorId && !sv.IsDeleted)
                .Include(sv => sv.Supervisor)
                .Include(sv => sv.Vehicle)
                .Include(sv => sv.AssignedByAdmin)
                .Include(sv => sv.ApprovedByAdmin)
                .OrderByDescending(sv => sv.StartTimeUtc)
                .ToListAsync();
        }

        public async Task<IEnumerable<Supervisor>> GetAvailableSupervisorsForVehicleAsync(Guid vehicleId, DateTime startTime, DateTime? endTime)
        {
            // Get all active supervisors
            var allSupervisors = await _context.Supervisors
                .Where(s => !s.IsDeleted && s.Status == SupervisorStatus.Active)
                .ToListAsync();

            // Get supervisors who have conflicting assignments (assigned to ANY vehicle during this period)
            var busySupervisorIds = await _context.SupervisorVehicles
                .Where(sv => !sv.IsDeleted &&
                            sv.StartTimeUtc < (endTime ?? DateTime.MaxValue) &&
                            (sv.EndTimeUtc == null || sv.EndTimeUtc > startTime))
                .Select(sv => sv.SupervisorId)
                .Distinct()
                .ToListAsync();

            // Return supervisors who are NOT in the busy list
            return allSupervisors.Where(s => !busySupervisorIds.Contains(s.Id));
        }
    }
}
