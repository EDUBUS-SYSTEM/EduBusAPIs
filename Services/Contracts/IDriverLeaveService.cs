using Services.Models.Driver;
using Data.Models.Enums;
using System.Collections.Generic;

namespace Services.Contracts
{
    public interface IDriverLeaveService
    {
        // Leave request management
        Task<DriverLeaveResponse> CreateLeaveRequestAsync(CreateLeaveRequestDto dto);
        Task<DriverLeaveResponse> UpdateLeaveRequestAsync(Guid leaveId, UpdateLeaveRequestDto dto);
        Task<DriverLeaveResponse> CancelLeaveRequestAsync(Guid leaveId, Guid driverId);
        
        // Approval workflow
        Task<DriverLeaveResponse> ApproveLeaveRequestAsync(Guid leaveId, ApproveLeaveRequestDto dto, Guid adminId);
        Task<DriverLeaveResponse> RejectLeaveRequestAsync(Guid leaveId, RejectLeaveRequestDto dto, Guid adminId);
        
        // Auto-replacement system
        Task<ReplacementSuggestionResponse> GenerateReplacementSuggestionsAsync(Guid leaveId);
        Task<ReplacementSuggestionResponse> AcceptReplacementSuggestionAsync(Guid leaveId, Guid suggestionId, Guid adminId);
        Task<ReplacementSuggestionResponse> RejectReplacementSuggestionAsync(Guid leaveId, Guid suggestionId, Guid adminId);
        
        // Conflict detection and resolution
        Task<IEnumerable<DriverLeaveConflictDto>> DetectConflictsAsync(Guid leaveId);
        Task<ConflictResolutionResponse> ResolveConflictsAsync(Guid leaveId, Guid adminId);
        
        // Queries
        Task<IEnumerable<DriverLeaveResponse>> GetDriverLeavesAsync(Guid driverId, DateTime? fromDate, DateTime? toDate);
        Task<DriverLeaveListResponse> GetDriverLeavesPaginatedAsync(
            Guid driverId, 
            DateTime? fromDate, 
            DateTime? toDate, 
            LeaveStatus? status,
            int page, 
            int perPage);
        Task<IEnumerable<DriverLeaveResponse>> GetPendingLeavesAsync();
        Task<DriverLeaveResponse?> GetLeaveByIdAsync(Guid leaveId);
        Task<IEnumerable<DriverLeaveResponse>> GetLeavesByStatusAsync(LeaveStatus status);
        
        // Paginated queries
        Task<DriverLeaveListResponse> GetLeaveRequestsAsync(DriverLeaveListRequest request);
        
        // Replacement info
        Task<DriverLeaveResponse?> GetActiveReplacementInfoByDriverIdAsync(Guid driverId);
        Task<IEnumerable<DriverReplacementMatchDto>> GetActiveReplacementMatchesAsync();
    }
}
