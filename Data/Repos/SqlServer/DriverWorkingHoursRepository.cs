using Data.Contexts.SqlServer;
using Data.Models;
using Data.Repos.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Data.Repos.SqlServer
{
    public class DriverWorkingHoursRepository : SqlRepository<DriverWorkingHours>, IDriverWorkingHoursRepository
    {
        private readonly EduBusSqlContext _context;

        public DriverWorkingHoursRepository(EduBusSqlContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<DriverWorkingHours>> GetByDriverIdAsync(Guid driverId)
        {
            return await _context.DriverWorkingHours
                .Where(w => w.DriverId == driverId && !w.IsDeleted)
                .ToListAsync();
        }

        public async Task<DriverWorkingHours?> GetByDriverAndDayAsync(Guid driverId, DayOfWeek dayOfWeek)
        {
            return await _context.DriverWorkingHours
                .FirstOrDefaultAsync(w => w.DriverId == driverId && w.DayOfWeek == dayOfWeek && !w.IsDeleted);
        }

        public async Task<bool> IsDriverAvailableAtTimeAsync(Guid driverId, DateTime dateTime)
        {
            var day = dateTime.DayOfWeek;
            var time = dateTime.TimeOfDay;

            var wh = await _context.DriverWorkingHours
                .FirstOrDefaultAsync(w => w.DriverId == driverId && w.DayOfWeek == day && !w.IsDeleted);

            if (wh == null || !wh.IsAvailable) return false;

            return wh.StartTime <= time && time <= wh.EndTime;
        }

        public async Task<IEnumerable<DriverWorkingHours>> GetAvailableDriversAtTimeAsync(DateTime dateTime)
        {
            var day = dateTime.DayOfWeek;
            var time = dateTime.TimeOfDay;

            return await _context.DriverWorkingHours
                .Where(w => !w.IsDeleted && w.DayOfWeek == day && w.IsAvailable && w.StartTime <= time && time <= w.EndTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<DriverWorkingHours>> GetDriversAvailableInTimeRangeAsync(DateTime startTime, DateTime endTime)
        {
            // Simplified: same day range check
            var day = startTime.DayOfWeek;
            var start = startTime.TimeOfDay;
            var end = endTime.TimeOfDay;

            return await _context.DriverWorkingHours
                .Where(w => !w.IsDeleted && w.DayOfWeek == day && w.IsAvailable && w.StartTime <= start && end <= w.EndTime)
                .ToListAsync();
        }
    }
}
