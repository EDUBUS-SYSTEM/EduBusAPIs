using Services.Models.Driver;
using Data.Models.Enums;
using System.Collections.Generic;

namespace Services.Contracts
{
    public interface IDriverLeaveService
    {
        // Leave request management
        Task<DriverLeaveResponse> CreateLeaveRequestAsync(CreateLeaveRequestDto dto);
        
        // Approval workflow
        Task<DriverLeaveResponse> ApproveLeaveRequestAsync(Guid leaveId, ApproveLeaveRequestDto dto, Guid adminId);
        Task<DriverLeaveResponse> RejectLeaveRequestAsync(Guid leaveId, RejectLeaveRequestDto dto, Guid adminId);
        
        // Auto-replacement system
        Task<ReplacementSuggestionResponse> GenerateReplacementSuggestionsAsync(Guid leaveId);
        
        // Queries
        Task<IEnumerable<DriverLeaveResponse>> GetDriverLeavesAsync(Guid driverId, DateTime? fromDate, DateTime? toDate);
        Task<DriverLeaveListResponse> GetDriverLeavesPaginatedAsync(
            Guid driverId, 
            DateTime? fromDate, 
            DateTime? toDate, 
            LeaveStatus? status,
            int page, 
            int perPage);
        Task<DriverLeaveResponse?> GetLeaveByIdAsync(Guid leaveId);
        
        // Paginated queries
        Task<DriverLeaveListResponse> GetLeaveRequestsAsync(DriverLeaveListRequest request);
        
        // Replacement info
        Task<DriverLeaveResponse?> GetActiveReplacementInfoByDriverIdAsync(Guid driverId);
        Task<IEnumerable<DriverReplacementMatchDto>> GetActiveReplacementMatchesAsync();
    }
}
