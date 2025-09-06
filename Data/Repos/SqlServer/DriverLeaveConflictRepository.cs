using Data.Contexts.SqlServer;
using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Data.Repos.SqlServer
{
    public class DriverLeaveConflictRepository : SqlRepository<DriverLeaveConflict>, IDriverLeaveConflictRepository
    {
        private readonly EduBusSqlContext _context;

        public DriverLeaveConflictRepository(EduBusSqlContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<DriverLeaveConflict>> GetByLeaveRequestIdAsync(Guid leaveRequestId)
        {
            return await _context.DriverLeaveConflicts
                .Where(c => c.LeaveRequestId == leaveRequestId && !c.IsDeleted)
                .ToListAsync();
        }

        public async Task<IEnumerable<DriverLeaveConflict>> GetByTripIdAsync(Guid tripId)
        {
            return await _context.DriverLeaveConflicts
                .Where(c => c.TripId == tripId && !c.IsDeleted)
                .ToListAsync();
        }

        public async Task<IEnumerable<DriverLeaveConflict>> GetConflictsBySeverityAsync(ConflictSeverity severity)
        {
            return await _context.DriverLeaveConflicts
                .Where(c => c.Severity == severity && !c.IsDeleted)
                .ToListAsync();
        }

        public async Task<IEnumerable<DriverLeaveConflict>> GetConflictsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.DriverLeaveConflicts
                .Where(c => !c.IsDeleted && c.TripStartTime <= endDate && c.TripEndTime >= startDate)
                .ToListAsync();
        }

        public async Task<bool> HasConflictsForDriverAsync(Guid driverId, DateTime startDate, DateTime endDate)
        {
            return await _context.DriverLeaveConflicts
                .Include(c => c.LeaveRequest)
                .AnyAsync(c => c.LeaveRequest.DriverId == driverId && !c.IsDeleted && c.TripStartTime <= endDate && c.TripEndTime >= startDate);
        }
    }
}
