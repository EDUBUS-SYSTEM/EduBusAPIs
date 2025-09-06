using Data.Models;
using Data.Models.Enums;

namespace Data.Repos.Interfaces
{
    public interface IDriverLeaveConflictRepository : ISqlRepository<DriverLeaveConflict>
    {
        Task<IEnumerable<DriverLeaveConflict>> GetByLeaveRequestIdAsync(Guid leaveRequestId);
        Task<IEnumerable<DriverLeaveConflict>> GetByTripIdAsync(Guid tripId);
        Task<IEnumerable<DriverLeaveConflict>> GetConflictsBySeverityAsync(ConflictSeverity severity);
        Task<IEnumerable<DriverLeaveConflict>> GetConflictsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<bool> HasConflictsForDriverAsync(Guid driverId, DateTime startDate, DateTime endDate);
    }
}
