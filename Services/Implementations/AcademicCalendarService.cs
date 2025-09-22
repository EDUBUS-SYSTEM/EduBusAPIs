using Data.Models;
using Data.Repos.Interfaces;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Services.Contracts;
using Utils;

namespace Services.Implementations
{
    public class AcademicCalendarService : IAcademicCalendarService
    {
        private readonly IDatabaseFactory _databaseFactory;
        private readonly ILogger<AcademicCalendarService> _logger;

        public AcademicCalendarService(IDatabaseFactory databaseFactory, ILogger<AcademicCalendarService> logger)
        {
            _databaseFactory = databaseFactory;
            _logger = logger;
        }

        public async Task<IEnumerable<AcademicCalendar>> QueryAcademicCalendarsAsync(
            string? academicYear,
            bool? activeOnly,
            int page,
            int perPage,
            string sortBy,
            string sortOrder)
        {
            try
            {
                if (page < 1) page = 1;
                if (perPage < 1) perPage = 20;

                var repository = _databaseFactory.GetRepositoryByType<IAcademicCalendarRepository>(DatabaseType.MongoDb);

                // Build filter
                var filters = new List<FilterDefinition<AcademicCalendar>>
                {
                    Builders<AcademicCalendar>.Filter.Eq(ac => ac.IsDeleted, false)
                };

                if (activeOnly == true)
                    filters.Add(Builders<AcademicCalendar>.Filter.Eq(ac => ac.IsActive, true));

                if (!string.IsNullOrWhiteSpace(academicYear))
                    filters.Add(Builders<AcademicCalendar>.Filter.Eq(ac => ac.AcademicYear, academicYear));

                var filter = filters.Count == 1 ? filters[0] : Builders<AcademicCalendar>.Filter.And(filters);

                // Build sort
                var desc = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
                SortDefinition<AcademicCalendar> sort = sortBy?.ToLowerInvariant() switch
                {
                    "academicyear" => desc ? Builders<AcademicCalendar>.Sort.Descending(x => x.AcademicYear) : Builders<AcademicCalendar>.Sort.Ascending(x => x.AcademicYear),
                    "startdate" => desc ? Builders<AcademicCalendar>.Sort.Descending(x => x.StartDate) : Builders<AcademicCalendar>.Sort.Ascending(x => x.StartDate),
                    "enddate" => desc ? Builders<AcademicCalendar>.Sort.Descending(x => x.EndDate) : Builders<AcademicCalendar>.Sort.Ascending(x => x.EndDate),
                    "name" => desc ? Builders<AcademicCalendar>.Sort.Descending(x => x.Name) : Builders<AcademicCalendar>.Sort.Ascending(x => x.Name),
                    "createdat" or _ => desc ? Builders<AcademicCalendar>.Sort.Descending(x => x.CreatedAt) : Builders<AcademicCalendar>.Sort.Ascending(x => x.CreatedAt)
                };

                var skip = (page - 1) * perPage;
                var items = await repository.FindByFilterAsync(filter, sort, skip, perPage);
                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying academic calendars with pagination/sorting");
                throw;
            }
        }

        public async Task<IEnumerable<AcademicCalendar>> GetAllAcademicCalendarsAsync()
        {
            try
            {
                var repository = _databaseFactory.GetRepositoryByType<IAcademicCalendarRepository>(DatabaseType.MongoDb);
                return await repository.FindAllAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all academic calendars");
                throw;
            }
        }

        public async Task<AcademicCalendar?> GetAcademicCalendarByIdAsync(Guid id)
        {
            try
            {
                var repository = _databaseFactory.GetRepositoryByType<IAcademicCalendarRepository>(DatabaseType.MongoDb);
                return await repository.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting academic calendar with id: {CalendarId}", id);
                throw;
            }
        }

        public async Task<AcademicCalendar> CreateAcademicCalendarAsync(AcademicCalendar academicCalendar)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(academicCalendar.AcademicYear))
                    throw new ArgumentException("Academic year is required");

                if (string.IsNullOrWhiteSpace(academicCalendar.Name))
                    throw new ArgumentException("Academic calendar name is required");

                if (academicCalendar.EndDate <= academicCalendar.StartDate)
                    throw new ArgumentException("End date must be greater than start date");

                ValidateAcademicCalendar(academicCalendar);

                var repository = _databaseFactory.GetRepositoryByType<IAcademicCalendarRepository>(DatabaseType.MongoDb);

                // Check for duplicate academic year
                var dupFilter = Builders<AcademicCalendar>.Filter.And(
                    Builders<AcademicCalendar>.Filter.Eq(x => x.IsDeleted, false),
                    Builders<AcademicCalendar>.Filter.Eq(x => x.AcademicYear, academicCalendar.AcademicYear)
                );
                var existingDup = await repository.FindByFilterAsync(dupFilter);
                if (existingDup.Any())
                    throw new InvalidOperationException("An academic calendar with the same year already exists.");

                return await repository.AddAsync(academicCalendar);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating academic calendar: {@AcademicCalendar}", academicCalendar);
                throw;
            }
        }

        public async Task<AcademicCalendar?> UpdateAcademicCalendarAsync(AcademicCalendar academicCalendar)
        {
            try
            {
                var repository = _databaseFactory.GetRepositoryByType<IAcademicCalendarRepository>(DatabaseType.MongoDb);
                var existingCalendar = await repository.FindAsync(academicCalendar.Id);
                if (existingCalendar == null)
                    return null;

                if (string.IsNullOrWhiteSpace(academicCalendar.AcademicYear))
                    throw new ArgumentException("Academic year is required");

                if (string.IsNullOrWhiteSpace(academicCalendar.Name))
                    throw new ArgumentException("Academic calendar name is required");

                if (academicCalendar.EndDate <= academicCalendar.StartDate)
                    throw new ArgumentException("End date must be greater than start date");

                ValidateAcademicCalendar(academicCalendar);

                // Check for duplicate academic year (exclude self)
                var dupFilter = Builders<AcademicCalendar>.Filter.And(
                    Builders<AcademicCalendar>.Filter.Eq(x => x.IsDeleted, false),
                    Builders<AcademicCalendar>.Filter.Ne(x => x.Id, academicCalendar.Id),
                    Builders<AcademicCalendar>.Filter.Eq(x => x.AcademicYear, academicCalendar.AcademicYear)
                );
                var existingDup = await repository.FindByFilterAsync(dupFilter);
                if (existingDup.Any())
                    throw new InvalidOperationException("An academic calendar with the same year already exists.");

                return await repository.UpdateAsync(academicCalendar);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating academic calendar: {@AcademicCalendar}", academicCalendar);
                throw;
            }
        }

        public async Task<AcademicCalendar?> DeleteAcademicCalendarAsync(Guid id)
        {
            try
            {
                var repository = _databaseFactory.GetRepositoryByType<IAcademicCalendarRepository>(DatabaseType.MongoDb);
                return await repository.DeleteAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting academic calendar with id: {CalendarId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<AcademicCalendar>> GetActiveAcademicCalendarsAsync()
        {
            try
            {
                var repository = _databaseFactory.GetRepositoryByType<IAcademicCalendarRepository>(DatabaseType.MongoDb);
                return await repository.GetActiveAcademicCalendarsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active academic calendars");
                throw;
            }
        }

        public async Task<AcademicCalendar?> GetAcademicCalendarByYearAsync(string academicYear)
        {
            try
            {
                var repository = _databaseFactory.GetRepositoryByType<IAcademicCalendarRepository>(DatabaseType.MongoDb);
                return await repository.GetAcademicCalendarByYearAsync(academicYear);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting academic calendar by year: {AcademicYear}", academicYear);
                throw;
            }
        }

        public async Task<bool> AcademicCalendarExistsAsync(Guid id)
        {
            try
            {
                var repository = _databaseFactory.GetRepositoryByType<IAcademicCalendarRepository>(DatabaseType.MongoDb);
                return await repository.ExistsAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if academic calendar exists: {CalendarId}", id);
                throw;
            }
        }

        public async Task<List<SchoolHoliday>> GetHolidaysAsync(Guid academicCalendarId)
        {
            try
            {
                var repository = _databaseFactory.GetRepositoryByType<IAcademicCalendarRepository>(DatabaseType.MongoDb);
                var calendar = await repository.FindAsync(academicCalendarId);
                if (calendar == null)
                    return new List<SchoolHoliday>();

                return calendar.Holidays.OrderBy(h => h.StartDate).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting holidays for academic calendar {CalendarId}", academicCalendarId);
                throw;
            }
        }

        public async Task<List<SchoolDay>> GetSchoolDaysAsync(Guid academicCalendarId)
        {
            try
            {
                var repository = _databaseFactory.GetRepositoryByType<IAcademicCalendarRepository>(DatabaseType.MongoDb);
                var calendar = await repository.FindAsync(academicCalendarId);
                if (calendar == null)
                    return new List<SchoolDay>();

                return calendar.SchoolDays.OrderBy(sd => sd.Date).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting school days for academic calendar {CalendarId}", academicCalendarId);
                throw;
            }
        }

        public async Task<bool> IsSchoolDayAsync(Guid academicCalendarId, DateTime date)
        {
            try
            {
                var repository = _databaseFactory.GetRepositoryByType<IAcademicCalendarRepository>(DatabaseType.MongoDb);
                var calendar = await repository.FindAsync(academicCalendarId);
                if (calendar == null)
                    return false;

                // Check if date is in calendar range
                if (date < calendar.StartDate || date > calendar.EndDate)
                    return false;

                // Check if date is a holiday
                if (calendar.Holidays.Any(h => date >= h.StartDate && date <= h.EndDate))
                    return false;

                // Check if date is explicitly marked as non-school day
                var schoolDay = calendar.SchoolDays.FirstOrDefault(sd => sd.Date.Date == date.Date);
                if (schoolDay != null)
                    return schoolDay.IsSchoolDay;

                // Default: assume it's a school day if not explicitly marked otherwise
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if date is school day for academic calendar {CalendarId}", academicCalendarId);
                throw;
            }
        }

        public async Task<bool> IsHolidayAsync(Guid academicCalendarId, DateTime date)
        {
            try
            {
                var repository = _databaseFactory.GetRepositoryByType<IAcademicCalendarRepository>(DatabaseType.MongoDb);
                var calendar = await repository.FindAsync(academicCalendarId);
                if (calendar == null)
                    return false;

                return calendar.Holidays.Any(h => date >= h.StartDate && date <= h.EndDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if date is holiday for academic calendar {CalendarId}", academicCalendarId);
                throw;
            }
        }

        public async Task<AcademicCalendar?> AddHolidayAsync(Guid academicCalendarId, SchoolHoliday holiday)
        {
            try
            {
                var repository = _databaseFactory.GetRepositoryByType<IAcademicCalendarRepository>(DatabaseType.MongoDb);
                var calendar = await repository.FindAsync(academicCalendarId);
                if (calendar == null)
                    return null;

                if (string.IsNullOrWhiteSpace(holiday.Name))
                    throw new ArgumentException("Holiday name is required");

                if (holiday.EndDate < holiday.StartDate)
                    throw new ArgumentException("Holiday end date must be greater than or equal to start date");

                // Check for overlapping holidays
                var overlappingHoliday = calendar.Holidays.FirstOrDefault(h => 
                    (holiday.StartDate >= h.StartDate && holiday.StartDate <= h.EndDate) ||
                    (holiday.EndDate >= h.StartDate && holiday.EndDate <= h.EndDate) ||
                    (holiday.StartDate <= h.StartDate && holiday.EndDate >= h.EndDate));

                if (overlappingHoliday != null)
                    throw new InvalidOperationException($"Holiday overlaps with existing holiday: {overlappingHoliday.Name}");

                calendar.Holidays.Add(holiday);
                return await repository.UpdateAsync(calendar);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding holiday to academic calendar {CalendarId}", academicCalendarId);
                throw;
            }
        }

        public async Task<AcademicCalendar?> AddSchoolDayAsync(Guid academicCalendarId, SchoolDay schoolDay)
        {
            try
            {
                var repository = _databaseFactory.GetRepositoryByType<IAcademicCalendarRepository>(DatabaseType.MongoDb);
                var calendar = await repository.FindAsync(academicCalendarId);
                if (calendar == null)
                    return null;

                // Check if school day already exists for this date
                var existingSchoolDay = calendar.SchoolDays.FirstOrDefault(sd => sd.Date.Date == schoolDay.Date.Date);
                if (existingSchoolDay != null)
                {
                    // Update existing school day
                    existingSchoolDay.IsSchoolDay = schoolDay.IsSchoolDay;
                    existingSchoolDay.Description = schoolDay.Description;
                }
                else
                {
                    // Add new school day
                    calendar.SchoolDays.Add(schoolDay);
                }

                return await repository.UpdateAsync(calendar);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding school day to academic calendar {CalendarId}", academicCalendarId);
                throw;
            }
        }

        public async Task<AcademicCalendar?> RemoveHolidayAsync(Guid academicCalendarId, DateTime date)
        {
            try
            {
                var repository = _databaseFactory.GetRepositoryByType<IAcademicCalendarRepository>(DatabaseType.MongoDb);
                var calendar = await repository.FindAsync(academicCalendarId);
                if (calendar == null)
                    return null;

                var holidayToRemove = calendar.Holidays.FirstOrDefault(h => 
                    date >= h.StartDate && date <= h.EndDate);

                if (holidayToRemove != null)
                {
                    calendar.Holidays.Remove(holidayToRemove);
                    return await repository.UpdateAsync(calendar);
                }

                return calendar;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing holiday from academic calendar {CalendarId}", academicCalendarId);
                throw;
            }
        }

        public async Task<AcademicCalendar?> RemoveSchoolDayAsync(Guid academicCalendarId, DateTime date)
        {
            try
            {
                var repository = _databaseFactory.GetRepositoryByType<IAcademicCalendarRepository>(DatabaseType.MongoDb);
                var calendar = await repository.FindAsync(academicCalendarId);
                if (calendar == null)
                    return null;

                var schoolDayToRemove = calendar.SchoolDays.FirstOrDefault(sd => sd.Date.Date == date.Date);
                if (schoolDayToRemove != null)
                {
                    calendar.SchoolDays.Remove(schoolDayToRemove);
                    return await repository.UpdateAsync(calendar);
                }

                return calendar;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing school day from academic calendar {CalendarId}", academicCalendarId);
                throw;
            }
        }

        private static void ValidateAcademicCalendar(AcademicCalendar calendar)
        {
            // Validate semesters
            foreach (var semester in calendar.Semesters)
            {
                if (string.IsNullOrWhiteSpace(semester.Name))
                    throw new ArgumentException("Semester name is required");

                if (string.IsNullOrWhiteSpace(semester.Code))
                    throw new ArgumentException("Semester code is required");

                if (semester.EndDate <= semester.StartDate)
                    throw new ArgumentException($"Semester '{semester.Name}' end date must be greater than start date");

                // Check if semester is within academic calendar range
                if (semester.StartDate < calendar.StartDate || semester.EndDate > calendar.EndDate)
                    throw new ArgumentException($"Semester '{semester.Name}' must be within academic calendar date range");
            }

            // Validate holidays
            foreach (var holiday in calendar.Holidays)
            {
                if (string.IsNullOrWhiteSpace(holiday.Name))
                    throw new ArgumentException("Holiday name is required");

                if (holiday.EndDate < holiday.StartDate)
                    throw new ArgumentException($"Holiday '{holiday.Name}' end date must be greater than or equal to start date");
            }

            // Validate school days
            foreach (var schoolDay in calendar.SchoolDays)
            {
                if (schoolDay.Date < calendar.StartDate || schoolDay.Date > calendar.EndDate)
                    throw new ArgumentException($"School day on {schoolDay.Date:yyyy-MM-dd} must be within academic calendar date range");
            }
        }
    }
}
