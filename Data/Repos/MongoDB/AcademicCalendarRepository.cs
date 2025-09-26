using Data.Models;
using Data.Repos.Interfaces;
using MongoDB.Driver;

namespace Data.Repos.MongoDB
{
    public class AcademicCalendarRepository : MongoRepository<AcademicCalendar>, IAcademicCalendarRepository
    {
        public AcademicCalendarRepository(IMongoDatabase database) : base(database, "academic_calendars")
        {
        }

        public async Task<IEnumerable<AcademicCalendar>> GetActiveAcademicCalendarsAsync()
        {
            var filter = Builders<AcademicCalendar>.Filter.And(
                Builders<AcademicCalendar>.Filter.Eq(ac => ac.IsDeleted, false),
                Builders<AcademicCalendar>.Filter.Eq(ac => ac.IsActive, true)
            );

            return await FindByFilterAsync(filter);
        }

        public async Task<AcademicCalendar?> GetAcademicCalendarByYearAsync(string academicYear)
        {
            var filter = Builders<AcademicCalendar>.Filter.And(
                Builders<AcademicCalendar>.Filter.Eq(ac => ac.IsDeleted, false),
                Builders<AcademicCalendar>.Filter.Eq(ac => ac.AcademicYear, academicYear)
            );

            var calendars = await FindByFilterAsync(filter);
            return calendars.FirstOrDefault();
        }

        public async Task<IEnumerable<AcademicCalendar>> GetAcademicCalendarsInDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var filter = Builders<AcademicCalendar>.Filter.And(
                Builders<AcademicCalendar>.Filter.Eq(ac => ac.IsDeleted, false),
                Builders<AcademicCalendar>.Filter.Or(
                    Builders<AcademicCalendar>.Filter.And(
                        Builders<AcademicCalendar>.Filter.Lte(ac => ac.StartDate, endDate),
                        Builders<AcademicCalendar>.Filter.Gte(ac => ac.EndDate, startDate)
                    )
                )
            );

            return await FindByFilterAsync(filter);
        }

        public async Task<bool> IsAcademicYearExistsAsync(string academicYear)
        {
            var filter = Builders<AcademicCalendar>.Filter.And(
                Builders<AcademicCalendar>.Filter.Eq(ac => ac.IsDeleted, false),
                Builders<AcademicCalendar>.Filter.Eq(ac => ac.AcademicYear, academicYear)
            );

            var calendars = await FindByFilterAsync(filter);
            return calendars.Any();
        }
    }
}
