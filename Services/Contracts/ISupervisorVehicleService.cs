using Services.Models.SupervisorVehicle;
using Services.Models.Common;
using Services.Models.UserAccount;

namespace Services.Contracts
{
    public interface ISupervisorVehicleService
    {
        Task<VehicleSupervisorsResponse?> GetSupervisorsByVehicleAsync(Guid vehicleId, bool? isActive);
        Task<SupervisorAssignmentResponse?> AssignSupervisorAsync(Guid vehicleId, SupervisorAssignmentRequest dto, Guid adminId);
        Task<SupervisorAssignmentResponse?> AssignSupervisorWithValidationAsync(Guid vehicleId, SupervisorAssignmentRequest dto, Guid adminId);
        Task<SupervisorAssignmentResponse?> UpdateAssignmentAsync(Guid assignmentId, UpdateSupervisorAssignmentRequest dto, Guid adminId);
        Task<SupervisorAssignmentResponse?> CancelAssignmentAsync(Guid assignmentId, string reason, Guid adminId);
        Task<BasicSuccessResponse?> DeleteAssignmentAsync(Guid assignmentId, Guid adminId);
        
        // Supervisor can view own assignments
        Task<SupervisorAssignmentListResponse> GetSupervisorAssignmentsAsync(Guid supervisorId, bool? isActive, DateTime? startDate, DateTime? endDate, int page, int perPage);
        Task<SupervisorAssignmentDto?> GetSupervisorCurrentVehicleAsync(Guid supervisorId);
    }
}
