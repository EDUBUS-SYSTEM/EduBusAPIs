using Data.Models;

namespace Data.Repos.Interfaces
{
    public interface ISupervisorVehicleRepository : ISqlRepository<SupervisorVehicle>
    {
        Task<IEnumerable<SupervisorVehicle>> GetByVehicleIdAsync(Guid vehicleId, bool? isActive);
        Task<SupervisorVehicle?> GetAssignmentAsync(Guid vehicleId, Guid supervisorId);
        Task<SupervisorVehicle> AssignSupervisorAsync(SupervisorVehicle entity);
        Task<bool> IsSupervisorAlreadyAssignedAsync(Guid vehicleId, Guid supervisorId, bool onlyActive = true);
        
        Task<bool> HasTimeConflictAsync(Guid supervisorId, DateTime startTime, DateTime? endTime, Guid? excludeAssignmentId = null);
        Task<bool> HasVehicleTimeConflictAsync(Guid vehicleId, DateTime startTime, DateTime? endTime, Guid? excludeAssignmentId = null);
        
        Task<IEnumerable<SupervisorVehicle>> GetActiveAssignmentsBySupervisorAsync(Guid supervisorId);
        Task<IEnumerable<SupervisorVehicle>> GetActiveAssignmentsByVehicleAsync(Guid vehicleId);
        
        Task<SupervisorVehicle?> GetActiveSupervisorVehicleForVehicleByDateAsync(Guid vehicleId, DateTime serviceDate);
        Task<SupervisorVehicle?> GetActiveSupervisorForVehicleAsync(Guid vehicleId);
        
        Task<IEnumerable<SupervisorVehicle>> GetBySupervisorIdAsync(Guid supervisorId);
        
        // Get supervisors that are NOT assigned to ANY vehicle during the specified time period
        Task<IEnumerable<Supervisor>> GetAvailableSupervisorsForVehicleAsync(Guid vehicleId, DateTime startTime, DateTime? endTime);
    }
}
