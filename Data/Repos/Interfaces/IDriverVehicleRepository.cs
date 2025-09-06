using Data.Models;

namespace Data.Repos.Interfaces
{
    public interface IDriverVehicleRepository : ISqlRepository<DriverVehicle>
    {
        Task<IEnumerable<DriverVehicle>> GetByVehicleIdAsync(Guid vehicleId, bool? isActive);
        Task<DriverVehicle?> GetAssignmentAsync(Guid vehicleId, Guid driverId);
        Task<DriverVehicle> AssignDriverAsync(DriverVehicle entity);
        Task<bool> IsDriverAlreadyAssignedAsync(Guid vehicleId, Guid driverId, bool onlyActive = true);
        
        Task<bool> HasTimeConflictAsync(Guid driverId, DateTime startTime, DateTime? endTime);
        Task<bool> HasVehicleTimeConflictAsync(Guid vehicleId, DateTime startTime, DateTime? endTime);
        Task<IEnumerable<DriverVehicle>> GetActiveAssignmentsByDriverAsync(Guid driverId);
        Task<IEnumerable<DriverVehicle>> GetActiveAssignmentsByVehicleAsync(Guid vehicleId);
        
        Task<IEnumerable<Driver>> FindAvailableDriversAsync(DateTime startTime, DateTime endTime, Guid? routeId = null);
        Task<IEnumerable<Vehicle>> FindAvailableVehiclesAsync(DateTime startTime, DateTime endTime, int minCapacity = 0);
        Task<double> CalculateDriverRouteFamiliarityAsync(Guid driverId, Guid routeId);
        Task<double> CalculateDriverPerformanceScoreAsync(Guid driverId);
    }
}
