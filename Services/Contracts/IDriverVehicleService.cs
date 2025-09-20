using Services.Models.DriverVehicle;
using Data.Models.Enums;
using Services.Models.Driver;
using Services.Models.UserAccount;

namespace Services.Contracts
{
    public interface IDriverVehicleService
    {
        Task<VehicleDriversResponse?> GetDriversByVehicleAsync(Guid vehicleId, bool? isActive);
        Task<IEnumerable<DriverInfoDto>> GetDriversNotAssignedToVehicleAsync(Guid vehicleId, DateTime start, DateTime end);
        Task<DriverAssignmentResponse?> AssignDriverAsync(Guid vehicleId, DriverAssignmentRequest dto, Guid adminId);
        
        Task<DriverAssignmentResponse?> AssignDriverWithValidationAsync(Guid vehicleId, DriverAssignmentRequest dto, Guid adminId);
        Task<DriverAssignmentResponse?> UpdateAssignmentAsync(Guid assignmentId, UpdateAssignmentRequest dto, Guid adminId);
        Task<DriverAssignmentResponse?> CancelAssignmentAsync(Guid assignmentId, string reason, Guid adminId);
        Task<BasicSuccessResponse?> DeleteAssignmentAsync(Guid assignmentId, Guid adminId);


        Task<IEnumerable<AssignmentConflictDto>> DetectAssignmentConflictsAsync(Guid vehicleId, DateTime startTime, DateTime endTime);
        Task<ReplacementSuggestionResponse> SuggestReplacementAsync(Guid assignmentId, Guid adminId);
        Task<bool> AcceptReplacementSuggestionAsync(Guid assignmentId, Guid suggestionId, Guid adminId);
        
        Task<DriverAssignmentResponse?> ApproveAssignmentAsync(Guid assignmentId, Guid adminId, string? note);
        Task<DriverAssignmentResponse?> RejectAssignmentAsync(Guid assignmentId, Guid adminId, string reason);
        
        // New methods for advanced filtering and management
        Task<AssignmentListResponse> GetAssignmentsWithFiltersAsync(AssignmentListRequest request);
        Task<DriverAssignmentSummaryResponse> GetDriverAssignmentSummaryAsync(Guid driverId);
        Task<AssignmentListResponse> GetDriverAssignmentsAsync(Guid driverId, bool? isActive = null, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int perPage = 20);
        Task<DriverAssignmentResponse?> UpdateAssignmentStatusAsync(Guid assignmentId, DriverVehicleStatus status, Guid adminId, string? note = null);
    }
}
