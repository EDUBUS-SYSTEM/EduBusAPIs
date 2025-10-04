using Data.Contexts.MongoDB;
using Data.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Data.SeedConfiguration
{
    public static class AcademicCalendarSeed
    {
        public static async Task SeedAsync(EduBusMongoContext mongoContext, ILogger logger)
        {
            var database = mongoContext.Database;
            var calendars = database.GetCollection<AcademicCalendar>("academic_calendars");

            var legacySingular = database.GetCollection<AcademicCalendar>("academiccalendar");
            var legacyCamel    = database.GetCollection<AcademicCalendar>("academicCalendars");
            var legacySingularCount = await legacySingular.CountDocumentsAsync(FilterDefinition<AcademicCalendar>.Empty);
            var legacyCamelCount    = await legacyCamel.CountDocumentsAsync(FilterDefinition<AcademicCalendar>.Empty);
            var targetCount = await calendars.CountDocumentsAsync(FilterDefinition<AcademicCalendar>.Empty);

            if (targetCount == 0)
            {
                if (legacyCamelCount > 0)
                {
                    var docs = await legacyCamel.Find(FilterDefinition<AcademicCalendar>.Empty).ToListAsync();
                    if (docs.Any())
                    {
                        await calendars.InsertManyAsync(docs);
                        logger.LogInformation("Migrated {Count} calendars from legacy 'academicCalendars' to 'academic_calendars' collection", docs.Count);
                        return;
                    }
                }
                if (legacySingularCount > 0)
                {
                    var docs = await legacySingular.Find(FilterDefinition<AcademicCalendar>.Empty).ToListAsync();
                    if (docs.Any())
                    {
                        await calendars.InsertManyAsync(docs);
                        logger.LogInformation("Migrated {Count} calendars from legacy 'academiccalendar' to 'academic_calendars' collection", docs.Count);
                        return;
                    }
                }
            }

            if (targetCount > 0)
            {
                logger.LogInformation("AcademicCalendar seed skipped: collection already has {Count} documents", targetCount);
                return;
            }

            var calendarId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

            var calendar = new AcademicCalendar
            {
                Id = calendarId,
                AcademicYear = "2025-2026",
                Name = "EduBus Academic Calendar 2025-2026",
                StartDate = new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2026, 6, 30, 23, 59, 59, DateTimeKind.Utc),
                Semesters = new List<AcademicSemester>
                {
                    new AcademicSemester
                    {
                        Name = "Semester 1",
                        Code = "S1",
                        StartDate = new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                        EndDate = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                        IsActive = true
                    },
                    new AcademicSemester
                    {
                        Name = "Semester 2",
                        Code = "S2",
                        StartDate = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc),
                        EndDate = new DateTime(2026, 6, 30, 23, 59, 59, DateTimeKind.Utc),
                        IsActive = true
                    }
                },
                Holidays = new List<SchoolHoliday>
                {
                    new SchoolHoliday
                    {
                        Name = "Tet Holiday",
                        StartDate = new DateTime(2026, 2, 8, 0, 0, 0, DateTimeKind.Utc),
                        EndDate = new DateTime(2026, 2, 16, 23, 59, 59, DateTimeKind.Utc),
                        Description = "Lunar New Year",
                        IsRecurring = false
                    },
                    new SchoolHoliday
                    {
                        Name = "National Day",
                        StartDate = new DateTime(2025, 9, 2, 0, 0, 0, DateTimeKind.Utc),
                        EndDate = new DateTime(2025, 9, 2, 23, 59, 59, DateTimeKind.Utc),
                        Description = "Vietnam National Day",
                        IsRecurring = true
                    }
                },
                SchoolDays = new List<SchoolDay>
                {
                    new SchoolDay { Date = new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc), IsSchoolDay = true, Description = "Opening ceremony" },
                    new SchoolDay { Date = new DateTime(2025, 9, 2, 0, 0, 0, DateTimeKind.Utc), IsSchoolDay = false, Description = "National Day" },
                    new SchoolDay { Date = new DateTime(2025, 9, 3, 0, 0, 0, DateTimeKind.Utc), IsSchoolDay = true, Description = "Regular class" }
                },
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await calendars.InsertOneAsync(calendar);
            logger.LogInformation("Seeded AcademicCalendar for year {AcademicYear}", calendar.AcademicYear);
        }
    }
}


