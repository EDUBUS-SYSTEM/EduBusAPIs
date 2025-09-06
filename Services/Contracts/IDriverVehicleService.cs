using Services.Models.DriverVehicle;
using Data.Models.Enums;
using Services.Models.Driver;

namespace Services.Contracts
{
    public interface IDriverVehicleService
    {
        Task<VehicleDriversResponse?> GetDriversByVehicleAsync(Guid vehicleId, bool? isActive);
        Task<DriverAssignmentResponse?> AssignDriverAsync(Guid vehicleId, DriverAssignmentRequest dto, Guid adminId);
        
        Task<DriverAssignmentResponse?> AssignDriverWithValidationAsync(Guid vehicleId, DriverAssignmentRequest dto, Guid adminId);
        Task<DriverAssignmentResponse?> UpdateAssignmentAsync(Guid assignmentId, UpdateAssignmentRequest dto, Guid adminId);
        Task<DriverAssignmentResponse?> CancelAssignmentAsync(Guid assignmentId, string reason, Guid adminId);
        
        Task<IEnumerable<AssignmentConflictDto>> DetectAssignmentConflictsAsync(Guid vehicleId, DateTime startTime, DateTime endTime);
        Task<ReplacementSuggestionResponse> SuggestReplacementAsync(Guid assignmentId, Guid adminId);
        Task<bool> AcceptReplacementSuggestionAsync(Guid assignmentId, Guid suggestionId, Guid adminId);
        
        Task<DriverAssignmentResponse?> ApproveAssignmentAsync(Guid assignmentId, Guid adminId, string? note);
        Task<DriverAssignmentResponse?> RejectAssignmentAsync(Guid assignmentId, Guid adminId, string reason);
    }
}
