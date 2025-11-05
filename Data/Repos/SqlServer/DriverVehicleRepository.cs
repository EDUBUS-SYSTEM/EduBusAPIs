using Data.Contexts.SqlServer;
using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Data.Repos.SqlServer
{
    public class DriverVehicleRepository : SqlRepository<DriverVehicle>, IDriverVehicleRepository
    {
        private readonly EduBusSqlContext _context;

        public DriverVehicleRepository(EduBusSqlContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<DriverVehicle>> GetByVehicleIdAsync(Guid vehicleId, bool? isActive)
        {
            var query = _context.DriverVehicles
                .Include(dv => dv.Driver)
                .Where(dv => dv.VehicleId == vehicleId && !dv.IsDeleted)
                .AsQueryable();

            if (isActive.HasValue)
            {
                var now = DateTime.UtcNow;

                if (isActive.Value)
                {
                    query = query.Where(dv => dv.Status == DriverVehicleStatus.Assigned &&
                                             dv.StartTimeUtc <= now &&
                                             (!dv.EndTimeUtc.HasValue || dv.EndTimeUtc > now));
                }
                else
                {
                    query = query.Where(dv => dv.Status == DriverVehicleStatus.Unassigned ||
                                             (dv.EndTimeUtc.HasValue && dv.EndTimeUtc <= now));
                }
            }

            return await query.ToListAsync();
        }

        public async Task<List<Driver>> GetDriversNotAssignedToVehicleAsync(Guid vehicleId, DateTime start, DateTime end)
        {
            // Get drivers that are NOT assigned to ANY vehicle during this time period
            // This prevents drivers from being double-booked across multiple vehicles
            var q = _context.Drivers
                .AsNoTracking()
                .Where(d => !d.IsDeleted)
                .Where(d => !_context.DriverVehicles.Any(a =>
                    a.DriverId == d.Id &&
                    !a.IsDeleted &&
                    a.Status == DriverVehicleStatus.Assigned &&
                    a.StartTimeUtc < end &&
                    (a.EndTimeUtc == null || a.EndTimeUtc > start)
                ));

            return await q.ToListAsync();
        }
        public async Task<DriverVehicle?> GetAssignmentAsync(Guid vehicleId, Guid driverId)
        {
            return await _context.DriverVehicles
                .Include(dv => dv.Driver)
                .FirstOrDefaultAsync(dv => dv.VehicleId == vehicleId &&
                                           dv.DriverId == driverId &&
                                           !dv.IsDeleted);
        }

        public async Task<DriverVehicle> AssignDriverAsync(DriverVehicle entity)
        {
            entity.CreatedAt = DateTime.UtcNow;
            await _context.DriverVehicles.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> IsDriverAlreadyAssignedAsync(Guid vehicleId, Guid driverId, bool onlyActive = true)
        {
            var query = _context.DriverVehicles
                .Where(dv => dv.VehicleId == vehicleId &&
                             dv.DriverId == driverId &&
                             !dv.IsDeleted);

            if (onlyActive)
            {
                var now = DateTime.UtcNow;
                query = query.Where(dv => dv.StartTimeUtc <= now &&
                                         (!dv.EndTimeUtc.HasValue || dv.EndTimeUtc >= now));
            }

            return await query.AnyAsync();
        }

        private static bool IsOverlapping(DateTime aStart, DateTime? aEnd, DateTime bStart, DateTime? bEnd)
        {
            var aEndVal = aEnd ?? DateTime.MaxValue;
            var bEndVal = bEnd ?? DateTime.MaxValue;
            return aStart < bEndVal && bStart < aEndVal;
        }

        public async Task<bool> HasTimeConflictAsync(Guid driverId, DateTime startTime, DateTime? endTime, Guid? excludeAssignmentId = null)
        {
            var query = _context.DriverVehicles
            .Where(dv => dv.DriverId == driverId && !dv.IsDeleted && dv.Status == DriverVehicleStatus.Assigned);

            // Exclude current assignment when updating
            if (excludeAssignmentId.HasValue)
            {
                query = query.Where(dv => dv.Id != excludeAssignmentId.Value);
            }

            // overlap: existing.Start < newEnd && newStart < existing.End (null end = infinite)
            var newEndVal = endTime ?? DateTime.MaxValue;
            return await query.AnyAsync(dv => dv.StartTimeUtc < newEndVal && startTime < (dv.EndTimeUtc ?? DateTime.MaxValue));
        }

        public async Task<bool> HasVehicleTimeConflictAsync(Guid vehicleId, DateTime startTime, DateTime? endTime, Guid? excludeAssignmentId = null)
        {
            var query = _context.DriverVehicles
            .Where(dv => dv.VehicleId == vehicleId && !dv.IsDeleted && dv.Status == DriverVehicleStatus.Assigned);

            // Exclude current assignment when updating
            if (excludeAssignmentId.HasValue)
            {
                query = query.Where(dv => dv.Id != excludeAssignmentId.Value);
            }

            var newEndVal = endTime ?? DateTime.MaxValue;
            return await query.AnyAsync(dv => dv.StartTimeUtc < newEndVal && startTime < (dv.EndTimeUtc ?? DateTime.MaxValue));
        }

        public async Task<IEnumerable<DriverVehicle>> GetActiveAssignmentsByDriverAsync(Guid driverId)
        {
            var now = DateTime.UtcNow;
            return await _context.DriverVehicles
                .Include(dv => dv.Vehicle)
                .Where(dv => dv.DriverId == driverId &&
                             dv.Status == DriverVehicleStatus.Assigned &&
                             dv.StartTimeUtc <= now &&
                             (!dv.EndTimeUtc.HasValue || dv.EndTimeUtc > now) &&
                             !dv.IsDeleted)
                .ToListAsync();
        }

        public async Task<IEnumerable<DriverVehicle>> GetActiveAssignmentsByVehicleAsync(Guid vehicleId)
        {
            var now = DateTime.UtcNow;
            return await _context.DriverVehicles
                .Include(dv => dv.Driver)
                .Where(dv => dv.VehicleId == vehicleId &&
                             dv.Status == DriverVehicleStatus.Assigned &&
                             dv.StartTimeUtc <= now &&
                             (!dv.EndTimeUtc.HasValue || dv.EndTimeUtc > now) &&
                             !dv.IsDeleted)
                .ToListAsync();
        }

        public async Task<IEnumerable<Driver>> FindAvailableDriversAsync(DateTime startTime, DateTime endTime, Guid? routeId = null)
        {
            // Drivers with no overlapping assignments in the window
            var newEndVal = endTime;
            var overlappingDriverIds = await _context.DriverVehicles
                .Where(dv => !dv.IsDeleted && dv.Status == DriverVehicleStatus.Assigned && dv.StartTimeUtc < newEndVal && startTime < (dv.EndTimeUtc ?? DateTime.MaxValue))
                .Select(dv => dv.DriverId)
                .Distinct()
                .ToListAsync();

            var drivers = await _context.Drivers
                .Include(d => d.DriverLicense)
                .Where(d => !d.IsDeleted && !overlappingDriverIds.Contains(d.Id))
                .ToListAsync();

            return drivers;
        }

        public async Task<IEnumerable<Vehicle>> FindAvailableVehiclesAsync(DateTime startTime, DateTime endTime, int minCapacity = 0)
        {
            var overlappingVehicleIds = await _context.DriverVehicles
                .Where(dv => !dv.IsDeleted && dv.StartTimeUtc < endTime && startTime < (dv.EndTimeUtc ?? DateTime.MaxValue))
                .Select(dv => dv.VehicleId)
                .Distinct()
                .ToListAsync();

            var vehicles = await _context.Vehicles
                .Where(v => !v.IsDeleted && v.Capacity >= minCapacity && !overlappingVehicleIds.Contains(v.Id))
                .ToListAsync();

            return vehicles;
        }

        public Task<double> CalculateDriverRouteFamiliarityAsync(Guid driverId, Guid routeId)
        {
            // Placeholder implementation; replace with real logic based on historical data
            return Task.FromResult(0.5);
        }

        public Task<double> CalculateDriverPerformanceScoreAsync(Guid driverId)
        {
            // Placeholder implementation; replace with metrics such as punctuality, incident rate, etc.
            return Task.FromResult(0.5);
        }

        public async Task<(IEnumerable<DriverVehicle> assignments, int totalCount)> GetAssignmentsWithFiltersAsync(
            Guid? driverId = null,
            Guid? vehicleId = null,
            int? status = null,
            bool? isPrimaryDriver = null,
            DateTime? startDateFrom = null,
            DateTime? startDateTo = null,
            DateTime? endDateFrom = null,
            DateTime? endDateTo = null,
            Guid? assignedByAdminId = null,
            Guid? approvedByAdminId = null,
            bool? isActive = null,
            bool? isUpcoming = null,
            bool? isCompleted = null,
            string? searchTerm = null,
            int page = 1,
            int perPage = 20,
            string? sortBy = "createdAt",
            string sortOrder = "desc")
        {
            var query = _context.DriverVehicles
                .Include(dv => dv.Driver)
                .Include(dv => dv.Vehicle)
                .Include(dv => dv.AssignedByAdmin)
                .Include(dv => dv.ApprovedByAdmin)
                .Where(dv => !dv.IsDeleted)
                .AsQueryable();

            // Apply filters
            if (driverId.HasValue)
                query = query.Where(dv => dv.DriverId == driverId.Value);

            if (vehicleId.HasValue)
                query = query.Where(dv => dv.VehicleId == vehicleId.Value);

            if (status.HasValue)
                query = query.Where(dv => (int)dv.Status == status.Value);

            if (isPrimaryDriver.HasValue)
                query = query.Where(dv => dv.IsPrimaryDriver == isPrimaryDriver.Value);

            if (startDateFrom.HasValue)
                query = query.Where(dv => dv.StartTimeUtc >= startDateFrom.Value);

            if (startDateTo.HasValue)
                query = query.Where(dv => dv.StartTimeUtc <= startDateTo.Value);

            if (endDateFrom.HasValue)
                query = query.Where(dv => dv.EndTimeUtc >= endDateFrom.Value);

            if (endDateTo.HasValue)
                query = query.Where(dv => dv.EndTimeUtc <= endDateTo.Value);

            if (assignedByAdminId.HasValue)
                query = query.Where(dv => dv.AssignedByAdminId == assignedByAdminId.Value);

            if (approvedByAdminId.HasValue)
                query = query.Where(dv => dv.ApprovedByAdminId == approvedByAdminId.Value);

            // Apply time-based filters
            var now = DateTime.UtcNow;
            if (isActive.HasValue)
            {
                if (isActive.Value)
                    query = query.Where(dv => dv.StartTimeUtc <= now && (!dv.EndTimeUtc.HasValue || dv.EndTimeUtc > now));
                else
                    query = query.Where(dv => dv.EndTimeUtc.HasValue && dv.EndTimeUtc <= now);
            }

            if (isUpcoming.HasValue && isUpcoming.Value)
                query = query.Where(dv => dv.StartTimeUtc > now);

            if (isCompleted.HasValue && isCompleted.Value)
                query = query.Where(dv => dv.EndTimeUtc.HasValue && dv.EndTimeUtc <= now);

            // Apply search
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var searchTermLower = searchTerm.ToLower();
                query = query.Where(dv => 
                    dv.Driver.FirstName.ToLower().Contains(searchTermLower) ||
                    dv.Driver.LastName.ToLower().Contains(searchTermLower) ||
                    dv.Driver.Email.ToLower().Contains(searchTermLower) ||
                    (dv.AssignmentReason != null && dv.AssignmentReason.ToLower().Contains(searchTermLower)));
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply sorting
            query = sortBy?.ToLower() switch
            {
                "starttime" => sortOrder?.ToLower() == "asc" ? query.OrderBy(dv => dv.StartTimeUtc) : query.OrderByDescending(dv => dv.StartTimeUtc),
                "endtime" => sortOrder?.ToLower() == "asc" ? query.OrderBy(dv => dv.EndTimeUtc) : query.OrderByDescending(dv => dv.EndTimeUtc),
                "drivername" => sortOrder?.ToLower() == "asc" ? query.OrderBy(dv => dv.Driver.FirstName).ThenBy(dv => dv.Driver.LastName) : query.OrderByDescending(dv => dv.Driver.FirstName).ThenByDescending(dv => dv.Driver.LastName),
                "vehicleplate" => sortOrder?.ToLower() == "asc" ? query.OrderBy(dv => dv.Vehicle.HashedLicensePlate) : query.OrderByDescending(dv => dv.Vehicle.HashedLicensePlate),
                "status" => sortOrder?.ToLower() == "asc" ? query.OrderBy(dv => dv.Status) : query.OrderByDescending(dv => dv.Status),
                _ => sortOrder?.ToLower() == "asc" ? query.OrderBy(dv => dv.CreatedAt) : query.OrderByDescending(dv => dv.CreatedAt)
            };

            // Apply pagination
            var assignments = await query
                .Skip((page - 1) * perPage)
                .Take(perPage)
                .ToListAsync();

            return (assignments, totalCount);
        }


        public async Task<IEnumerable<DriverVehicle>> GetDriverAssignmentsAsync(Guid driverId, bool? isActive = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.DriverVehicles
                .Include(dv => dv.Vehicle)
                .Include(dv => dv.AssignedByAdmin)
                .Include(dv => dv.ApprovedByAdmin)
                .Where(dv => dv.DriverId == driverId && !dv.IsDeleted)
                .AsQueryable();

            if (isActive.HasValue)
            {
                var now = DateTime.UtcNow;
                if (isActive.Value)
                    query = query.Where(dv => dv.StartTimeUtc <= now && (!dv.EndTimeUtc.HasValue || dv.EndTimeUtc > now));
                else
                    query = query.Where(dv => dv.EndTimeUtc.HasValue && dv.EndTimeUtc <= now);
            }

            if (startDate.HasValue)
                query = query.Where(dv => dv.StartTimeUtc >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(dv => dv.StartTimeUtc <= endDate.Value);

            return await query.OrderByDescending(dv => dv.StartTimeUtc).ToListAsync();
        }

        public async Task<Dictionary<Guid, DriverVehicle?>> GetPrimaryVehiclesForDriversAsync(IEnumerable<Guid> driverIds)
        {
            var driverIdsList = driverIds.ToList();
            if (!driverIdsList.Any()) return new Dictionary<Guid, DriverVehicle?>();

            var now = DateTime.UtcNow;
            var primaryVehicles = await _context.DriverVehicles
                .Include(dv => dv.Vehicle)
                .Where(dv => driverIdsList.Contains(dv.DriverId) &&
                           !dv.IsDeleted &&
                           dv.IsPrimaryDriver &&
                           dv.StartTimeUtc <= now &&
                           (!dv.EndTimeUtc.HasValue || dv.EndTimeUtc > now))
                .ToListAsync();

            var result = new Dictionary<Guid, DriverVehicle?>();
            foreach (var driverId in driverIdsList)
            {
                result[driverId] = primaryVehicles.FirstOrDefault(pv => pv.DriverId == driverId);
            }

            return result;
        }

        public async Task<DriverVehicle?> GetPrimaryVehicleForDriverAsync(Guid driverId)
        {
            var now = DateTime.UtcNow;
            return await _context.DriverVehicles
                .Include(dv => dv.Vehicle)
                .Where(dv => dv.DriverId == driverId &&
                           !dv.IsDeleted &&
                           dv.IsPrimaryDriver &&
                           dv.StartTimeUtc <= now &&
                           (!dv.EndTimeUtc.HasValue || dv.EndTimeUtc > now))
                .FirstOrDefaultAsync();
        }

        public async Task<DriverVehicle?> GetActivePrimaryDriverForVehicleAsync(Guid vehicleId)
        {
            var now = DateTime.UtcNow;
            return await _context.DriverVehicles
                .Include(dv => dv.Driver)
                .Where(dv => dv.VehicleId == vehicleId &&
                           !dv.IsDeleted &&
                           dv.IsPrimaryDriver &&
                           dv.Status == DriverVehicleStatus.Assigned &&
                           dv.StartTimeUtc <= now &&
                           (!dv.EndTimeUtc.HasValue || dv.EndTimeUtc > now))
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<DriverVehicle>> GetActiveDriverVehiclesByDateAsync(Guid driverId, DateTime serviceDate)
        {
            var startOfDay = serviceDate.Date;
            var endOfDay = startOfDay.AddDays(1);

            return await _context.DriverVehicles
                .Include(dv => dv.Vehicle)
                .Where(dv => dv.DriverId == driverId &&
                           !dv.IsDeleted &&
                           dv.Status == DriverVehicleStatus.Assigned &&
                           dv.StartTimeUtc < endOfDay &&
                           (!dv.EndTimeUtc.HasValue || dv.EndTimeUtc >= startOfDay))
                .ToListAsync();
        }

        public async Task<IEnumerable<DriverVehicle>> GetActiveDriverVehiclesByDateRangeAsync(Guid driverId, DateTime startDate, DateTime endDate)
        {
            var startOfRange = startDate.Date;
            var endOfRange = endDate.Date.AddDays(1);

            return await _context.DriverVehicles
                .Include(dv => dv.Vehicle)
                .Where(dv => dv.DriverId == driverId &&
                           !dv.IsDeleted &&
                           dv.Status == DriverVehicleStatus.Assigned &&
                           dv.StartTimeUtc < endOfRange &&
                           (!dv.EndTimeUtc.HasValue || dv.EndTimeUtc >= startOfRange))
                .OrderBy(dv => dv.StartTimeUtc)
                .ToListAsync();
        }
    }
}
