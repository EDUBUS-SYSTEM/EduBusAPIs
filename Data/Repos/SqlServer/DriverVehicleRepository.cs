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
    }
}
