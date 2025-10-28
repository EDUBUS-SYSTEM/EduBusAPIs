using Data.Contexts.SqlServer;
using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Data.Repos.SqlServer
{
    public class DriverLeaveRepository : SqlRepository<DriverLeaveRequest>, IDriverLeaveRepository
    {
        private readonly EduBusSqlContext _context;

        public DriverLeaveRepository(EduBusSqlContext context) : base(context)
        {
            _context = context;
        }

        public override async Task<DriverLeaveRequest?> FindAsync(Guid id)
        {
            return await _context.DriverLeaveRequests
                .Include(l => l.Driver)
                .Include(l => l.ApprovedByAdmin)
                .Include(l => l.SuggestedReplacementDriver)
                .Include(l => l.SuggestedReplacementVehicle)
                .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);
        }

        public async Task<IEnumerable<DriverLeaveRequest>> GetByDriverIdAsync(Guid driverId, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.DriverLeaveRequests
                .Include(l => l.Driver)
                .Where(l => l.DriverId == driverId && !l.IsDeleted)
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(l => l.EndDate >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(l => l.StartDate <= toDate.Value);

            return await query.ToListAsync();
        }

        public async Task<(IEnumerable<DriverLeaveRequest> items, int totalCount)> GetByDriverIdPaginatedAsync(
            Guid driverId, 
            DateTime? fromDate, 
            DateTime? toDate, 
            LeaveStatus? status,
            int page, 
            int perPage)
        {
            var query = _context.DriverLeaveRequests
                .Include(l => l.Driver)
                .Include(l => l.ApprovedByAdmin)
                .Include(l => l.SuggestedReplacementDriver)
                .Include(l => l.SuggestedReplacementVehicle)
                .Where(l => l.DriverId == driverId && !l.IsDeleted);

            // Apply date filters
            if (fromDate.HasValue)
                query = query.Where(l => l.EndDate >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(l => l.StartDate <= toDate.Value);
            
            // Apply status filter
            if (status.HasValue)
                query = query.Where(l => l.Status == status.Value);

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination with ordering (newest first)
            var items = await query
                .OrderByDescending(l => l.RequestedAt)
                .Skip((page - 1) * perPage)
                .Take(perPage)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<IEnumerable<DriverLeaveRequest>> GetPendingLeavesAsync()
        {
            return await _context.DriverLeaveRequests
                .Include(l => l.Driver)
                .Where(l => l.Status == LeaveStatus.Pending && !l.IsDeleted)
                .ToListAsync();
        }

        public async Task<IEnumerable<DriverLeaveRequest>> GetLeavesByStatusAsync(LeaveStatus status)
        {
            return await _context.DriverLeaveRequests
                .Include(l => l.Driver)
                .Where(l => l.Status == status && !l.IsDeleted)
                .ToListAsync();
        }

        public async Task<IEnumerable<DriverLeaveRequest>> GetLeavesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.DriverLeaveRequests
                .Include(l => l.Driver)
                .Where(l => !l.IsDeleted && l.StartDate <= endDate && l.EndDate >= startDate)
                .ToListAsync();
        }

        public async Task<bool> HasOverlappingLeaveAsync(Guid driverId, DateTime startDate, DateTime endDate)
        {
            return await _context.DriverLeaveRequests
                .AnyAsync(l => l.DriverId == driverId && !l.IsDeleted && l.StartDate <= endDate && l.EndDate >= startDate);
        }

        public async Task<IEnumerable<DriverLeaveRequest>> GetLeavesByDriverAndDateRangeAsync(Guid driverId, DateTime startDate, DateTime endDate)
        {
            return await _context.DriverLeaveRequests
                .Where(l => l.DriverId == driverId && !l.IsDeleted && l.StartDate <= endDate && l.EndDate >= startDate)
                .ToListAsync();
        }

    public async Task<DriverLeaveRequest?> GetActiveReplacementByDriverIdAsync(Guid driverId)
    {
        // Find leave request for this driver (DriverId = driverId)
        // with a replacement driver (SuggestedReplacementDriverId != null)
        // No date filtering here, let frontend check date overlap with assignment
        return await _context.DriverLeaveRequests
            .Include(lr => lr.Driver)
                .ThenInclude(d => d.DriverLicense)
            .Include(lr => lr.ApprovedByAdmin)
            .Include(lr => lr.SuggestedReplacementDriver)
                .ThenInclude(d => d.DriverLicense)
            .Include(lr => lr.SuggestedReplacementVehicle)
            .Where(lr => !lr.IsDeleted &&
                        lr.Status == LeaveStatus.Approved &&
                        lr.DriverId == driverId &&
                        lr.SuggestedReplacementDriverId != null)
            .OrderByDescending(lr => lr.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<DriverLeaveRequest>> GetActiveReplacementsAsync()
    {
        // Get all approved leave requests with replacement driver
        // Don't check vehicle ID as it can be null
        // No date filtering here, let frontend match with assignment dates
        return await _context.DriverLeaveRequests
            .Where(lr => !lr.IsDeleted &&
                        lr.Status == LeaveStatus.Approved &&
                        lr.SuggestedReplacementDriverId != null)
            .ToListAsync();
    }
    }
}
