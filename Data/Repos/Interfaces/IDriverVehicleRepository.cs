using Data.Models;

namespace Data.Repos.Interfaces
{
    public interface IDriverVehicleRepository : ISqlRepository<DriverVehicle>
    {
        Task<IEnumerable<DriverVehicle>> GetByVehicleIdAsync(Guid vehicleId, bool? isActive);
        Task<DriverVehicle?> GetAssignmentAsync(Guid vehicleId, Guid driverId);
        Task<DriverVehicle> AssignDriverAsync(DriverVehicle entity);
        Task<bool> IsDriverAlreadyAssignedAsync(Guid vehicleId, Guid driverId, bool onlyActive = true);
    }
}
