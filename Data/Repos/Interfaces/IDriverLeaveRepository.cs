using Data.Models;
using Data.Models.Enums;

namespace Data.Repos.Interfaces
{
    public interface IDriverLeaveRepository : ISqlRepository<DriverLeaveRequest>
    {
        Task<IEnumerable<DriverLeaveRequest>> GetByDriverIdAsync(Guid driverId, DateTime? fromDate, DateTime? toDate);
        Task<(IEnumerable<DriverLeaveRequest> items, int totalCount)> GetByDriverIdPaginatedAsync(
            Guid driverId, 
            DateTime? fromDate, 
            DateTime? toDate, 
            LeaveStatus? status,
            int page, 
            int perPage);
        Task<IEnumerable<DriverLeaveRequest>> GetPendingLeavesAsync();
        Task<IEnumerable<DriverLeaveRequest>> GetLeavesByStatusAsync(LeaveStatus status);
        Task<IEnumerable<DriverLeaveRequest>> GetLeavesByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<bool> HasOverlappingLeaveAsync(Guid driverId, DateTime startDate, DateTime endDate);
        Task<IEnumerable<DriverLeaveRequest>> GetLeavesByDriverAndDateRangeAsync(Guid driverId, DateTime startDate, DateTime endDate);
        Task<DriverLeaveRequest?> GetActiveReplacementByDriverIdAsync(Guid replacementDriverId);
        Task<IEnumerable<DriverLeaveRequest>> GetActiveReplacementsAsync();
    }
}
