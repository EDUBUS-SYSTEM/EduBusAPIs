using Data.Models;

namespace Data.Repos.Interfaces
{
    public interface IAcademicCalendarRepository : IMongoRepository<AcademicCalendar>
    {
        Task<IEnumerable<AcademicCalendar>> GetActiveAcademicCalendarsAsync();
        Task<AcademicCalendar?> GetAcademicCalendarByYearAsync(string academicYear);
        Task<IEnumerable<AcademicCalendar>> GetAcademicCalendarsInDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<bool> IsAcademicYearExistsAsync(string academicYear);
        Task<List<AcademicCalendar>> GetActiveAsync();
    }
}
