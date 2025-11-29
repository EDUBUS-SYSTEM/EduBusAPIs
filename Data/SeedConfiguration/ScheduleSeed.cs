using Data.Contexts.MongoDB;
using Data.Models;
using Data.Models.Enums;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Data.SeedConfiguration
{
    public static class ScheduleSeed
    {
        public static async Task SeedAsync(EduBusMongoContext mongoContext, ILogger logger)
        {
            var database = mongoContext.Database;
            var schedules = database.GetCollection<Schedule>("schedules");

            var legacy = database.GetCollection<Schedule>("schedule");
            var legacyCount = await legacy.CountDocumentsAsync(FilterDefinition<Schedule>.Empty);
            var targetCount = await schedules.CountDocumentsAsync(FilterDefinition<Schedule>.Empty);
            if (legacyCount > 0 && targetCount == 0)
            {
                var legacyDocs = await legacy.Find(FilterDefinition<Schedule>.Empty).ToListAsync();
                if (legacyDocs.Any())
                {
                    await schedules.InsertManyAsync(legacyDocs);
                    logger.LogInformation("Migrated {Count} schedules from legacy 'schedule' to 'schedules' collection", legacyDocs.Count);
                    return;
                }
            }

            if (targetCount > 0)
            {
                logger.LogInformation("Schedule seed skipped: collection already has {Count} documents", targetCount);
                return;
            }

            var schoolDayScheduleId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
            var examDayScheduleId   = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2");

            var seedItems = new List<Schedule>
            {
                new Schedule
                {
                    Id = schoolDayScheduleId,
                    Name = "Morning Route Schedule",
                    StartTime = "07:00:00",
                    EndTime = "08:30:00",
                    RRule = "FREQ=WEEKLY;BYDAY=MO,TU,WE,TH,FR",
                    Timezone = "UTC",
                    AcademicYear = "2025-2026",
                    EffectiveFrom = new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                    EffectiveTo = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                    Exceptions = new List<DateTime> { new DateTime(2025, 10, 20, 0, 0, 0, DateTimeKind.Utc) },
                    ScheduleType = "school_day",
                    TripType = TripType.Departure,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Schedule
                {
                    Id = examDayScheduleId,
                    Name = "Exam Day",
                    StartTime = "08:00:00",
                    EndTime = "10:00:00",
                    RRule = "FREQ=MONTHLY;BYMONTHDAY=15",
                    Timezone = "UTC",
                    AcademicYear = "2025-2026",
                    EffectiveFrom = new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                    EffectiveTo = new DateTime(2026, 6, 30, 23, 59, 59, DateTimeKind.Utc),
                    Exceptions = new List<DateTime>(),
                    ScheduleType = "exam_day",
                    TripType = TripType.Return,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            await schedules.InsertManyAsync(seedItems);
            logger.LogInformation("Seeded {Count} schedules for future Route/Trip integration", seedItems.Count);
        }
    }
}


