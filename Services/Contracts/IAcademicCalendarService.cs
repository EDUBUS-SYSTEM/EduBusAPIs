using Data.Models;

namespace Services.Contracts
{
    public interface IAcademicCalendarService
    {
        Task<IEnumerable<AcademicCalendar>> GetAllAcademicCalendarsAsync();
        Task<AcademicCalendar?> GetAcademicCalendarByIdAsync(Guid id);
        Task<AcademicCalendar> CreateAcademicCalendarAsync(AcademicCalendar academicCalendar);
        Task<AcademicCalendar?> UpdateAcademicCalendarAsync(AcademicCalendar academicCalendar);
        Task<AcademicCalendar?> DeleteAcademicCalendarAsync(Guid id);
        Task<IEnumerable<AcademicCalendar>> GetActiveAcademicCalendarsAsync();
        Task<AcademicCalendar?> GetAcademicCalendarByYearAsync(string academicYear);
        Task<bool> AcademicCalendarExistsAsync(Guid id);
        Task<IEnumerable<AcademicCalendar>> QueryAcademicCalendarsAsync(
            string? academicYear,
            bool? activeOnly,
            int page,
            int perPage,
            string sortBy,
            string sortOrder);
        Task<List<SchoolHoliday>> GetHolidaysAsync(Guid academicCalendarId);
        Task<List<SchoolDay>> GetSchoolDaysAsync(Guid academicCalendarId);
        Task<bool> IsSchoolDayAsync(Guid academicCalendarId, DateTime date);
        Task<bool> IsHolidayAsync(Guid academicCalendarId, DateTime date);
        Task<AcademicCalendar?> AddHolidayAsync(Guid academicCalendarId, SchoolHoliday holiday);
        Task<AcademicCalendar?> AddSchoolDayAsync(Guid academicCalendarId, SchoolDay schoolDay);
        Task<AcademicCalendar?> RemoveHolidayAsync(Guid academicCalendarId, DateTime date);
        Task<AcademicCalendar?> RemoveSchoolDayAsync(Guid academicCalendarId, DateTime date);
    }
}
