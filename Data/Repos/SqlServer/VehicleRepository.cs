using Data.Contexts.SqlServer;
using Data.Models;
using Data.Repos.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Data.Repos.SqlServer
{
    public class VehicleRepository : SqlRepository<Vehicle>, IVehicleRepository
    {
        private readonly EduBusSqlContext _context;

        public VehicleRepository(EduBusSqlContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Vehicle>> GetVehiclesAsync(
            string? status,
            int? minCapacity,
            Guid? adminId,
            int page,
            int perPage,
            string? sortBy,
            string sortOrder)
        {
            var query = _context.Vehicles
                .Where(v => !v.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(v => v.Status == status);

            if (minCapacity.HasValue)
                query = query.Where(v => v.Capacity >= minCapacity.Value);

            if (adminId.HasValue)
                query = query.Where(v => v.AdminId == adminId.Value);

            query = sortBy switch
            {
                "capacity" => (sortOrder == "asc") ? query.OrderBy(v => v.Capacity) : query.OrderByDescending(v => v.Capacity),
                "status" => (sortOrder == "asc") ? query.OrderBy(v => v.Status) : query.OrderByDescending(v => v.Status),
                "updatedAt" => (sortOrder == "asc") ? query.OrderBy(v => v.UpdatedAt) : query.OrderByDescending(v => v.UpdatedAt),
                _ => query.OrderByDescending(v => v.CreatedAt)
            };

            return await query
                .Skip((page - 1) * perPage)
                .Take(perPage)
                .ToListAsync();
        }
    }
}
