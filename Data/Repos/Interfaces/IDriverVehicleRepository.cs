using Data.Models;

namespace Data.Repos.Interfaces
{
    public interface IDriverVehicleRepository : ISqlRepository<DriverVehicle>
    {
        Task<IEnumerable<DriverVehicle>> GetByVehicleIdAsync(Guid vehicleId, bool? isActive);
        Task<List<Driver>> GetDriversNotAssignedToVehicleAsync(Guid vehicleId, DateTime start, DateTime end);
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
        
        // New methods for advanced filtering and pagination - using basic types only
        Task<(IEnumerable<DriverVehicle> assignments, int totalCount)> GetAssignmentsWithFiltersAsync(
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
            string sortOrder = "desc");
        
        Task<IEnumerable<DriverVehicle>> GetDriverAssignmentsAsync(Guid driverId, bool? isActive = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<Dictionary<Guid, DriverVehicle?>> GetPrimaryVehiclesForDriversAsync(IEnumerable<Guid> driverIds);
        Task<DriverVehicle?> GetPrimaryVehicleForDriverAsync(Guid driverId);
    }
}
