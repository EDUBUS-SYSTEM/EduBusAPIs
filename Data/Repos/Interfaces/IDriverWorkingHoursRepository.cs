using Data.Models;

namespace Data.Repos.Interfaces
{
    public interface IDriverWorkingHoursRepository : ISqlRepository<DriverWorkingHours>
    {
        Task<IEnumerable<DriverWorkingHours>> GetByDriverIdAsync(Guid driverId);
        Task<DriverWorkingHours?> GetByDriverAndDayAsync(Guid driverId, DayOfWeek dayOfWeek);
        Task<bool> IsDriverAvailableAtTimeAsync(Guid driverId, DateTime dateTime);
        Task<IEnumerable<DriverWorkingHours>> GetAvailableDriversAtTimeAsync(DateTime dateTime);
        Task<IEnumerable<DriverWorkingHours>> GetDriversAvailableInTimeRangeAsync(DateTime startTime, DateTime endTime);
    }
}
