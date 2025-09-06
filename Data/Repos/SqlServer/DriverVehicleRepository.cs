using Data.Contexts.SqlServer;
using Data.Models;
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
                    query = query.Where(dv => dv.StartTimeUtc <= now &&
                                             (!dv.EndTimeUtc.HasValue || dv.EndTimeUtc > now));
                }
                else
                {
                    query = query.Where(dv => dv.EndTimeUtc.HasValue && dv.EndTimeUtc <= now);
                }
            }

            return await query.ToListAsync();
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

        public async Task<bool> HasTimeConflictAsync(Guid driverId, DateTime startTime, DateTime? endTime)
        {
            var query = _context.DriverVehicles
                .Where(dv => dv.DriverId == driverId && !dv.IsDeleted);

            // overlap: existing.Start < newEnd && newStart < existing.End (null end = infinite)
            var newEndVal = endTime ?? DateTime.MaxValue;
            return await query.AnyAsync(dv => dv.StartTimeUtc < newEndVal && startTime < (dv.EndTimeUtc ?? DateTime.MaxValue));
        }

        public async Task<bool> HasVehicleTimeConflictAsync(Guid vehicleId, DateTime startTime, DateTime? endTime)
        {
            var query = _context.DriverVehicles
                .Where(dv => dv.VehicleId == vehicleId && !dv.IsDeleted);
            var newEndVal = endTime ?? DateTime.MaxValue;
            return await query.AnyAsync(dv => dv.StartTimeUtc < newEndVal && startTime < (dv.EndTimeUtc ?? DateTime.MaxValue));
        }

        public async Task<IEnumerable<DriverVehicle>> GetActiveAssignmentsByDriverAsync(Guid driverId)
        {
            var now = DateTime.UtcNow;
            return await _context.DriverVehicles
                .Include(dv => dv.Vehicle)
                .Where(dv => dv.DriverId == driverId &&
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
                .Where(dv => !dv.IsDeleted && dv.StartTimeUtc < newEndVal && startTime < (dv.EndTimeUtc ?? DateTime.MaxValue))
                .Select(dv => dv.DriverId)
                .Distinct()
                .ToListAsync();

            var drivers = await _context.Drivers
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
    }
}
