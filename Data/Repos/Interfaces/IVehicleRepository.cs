using Data.Models;

namespace Data.Repos.Interfaces
{
    public interface IVehicleRepository : ISqlRepository<Vehicle>
    {
        Task<IEnumerable<Vehicle>> GetVehiclesAsync(
            string? status = null,
            int? minCapacity = null,
            Guid? adminId = null,
            int page = 1,
            int perPage = 20,
            string? sortBy = null,
            string sortOrder = null,
            List<Guid>? exceptionIds = null);
        Task<bool> IsVehicleActiveAsync(Guid vehicleId);
    }

}
