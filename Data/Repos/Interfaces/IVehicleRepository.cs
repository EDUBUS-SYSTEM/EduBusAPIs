using Data.Models;

namespace Data.Repos.Interfaces
{
    public interface IVehicleRepository : ISqlRepository<Vehicle>
    {
        Task<IEnumerable<Vehicle>> GetVehiclesAsync(
            string? status,
            int? minCapacity,
            Guid? adminId,
            int page,
            int perPage,
            string? sortBy,
            string sortOrder);
    }
}
