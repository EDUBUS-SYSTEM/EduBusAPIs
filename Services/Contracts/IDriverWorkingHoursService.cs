using Services.Models.Driver;

namespace Services.Contracts
{
    public interface IDriverWorkingHoursService
    {
        // Working hours management
        Task<DriverWorkingHoursResponse> CreateWorkingHoursAsync(CreateWorkingHoursDto dto);
        Task<DriverWorkingHoursResponse> UpdateWorkingHoursAsync(Guid workingHoursId, UpdateWorkingHoursDto dto);
        Task<bool> DeleteWorkingHoursAsync(Guid workingHoursId);
        
        // Availability queries
        Task<bool> IsDriverAvailableAtTimeAsync(Guid driverId, DateTime dateTime);
        Task<IEnumerable<DriverResponse>> GetAvailableDriversAtTimeAsync(DateTime dateTime);
        Task<IEnumerable<DriverResponse>> GetDriversAvailableInTimeRangeAsync(DateTime startTime, DateTime endTime);
        
        // Working hours queries
        Task<IEnumerable<DriverWorkingHoursResponse>> GetWorkingHoursByDriverAsync(Guid driverId);
        Task<DriverWorkingHoursResponse?> GetWorkingHoursByDriverAndDayAsync(Guid driverId, DayOfWeek dayOfWeek);
        
        // Bulk operations
        Task<IEnumerable<DriverWorkingHoursResponse>> SetDefaultWorkingHoursAsync(Guid driverId);
        Task<IEnumerable<DriverWorkingHoursResponse>> CopyWorkingHoursFromDriverAsync(Guid sourceDriverId, Guid targetDriverId);
    }
}
