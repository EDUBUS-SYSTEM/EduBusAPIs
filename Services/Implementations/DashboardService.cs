using Data.Models;
using Data.Repos.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Services.Contracts;
using Services.Models.Dashboard;
using Route = Data.Models.Route;
using Data.Models.Enums;
using Utils;

namespace Services.Implementations
{
    public class DashboardService : IDashboardService
    {
        private readonly ILogger<DashboardService> _logger;
        private readonly IMongoDatabase _mongoDatabase;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IAcademicCalendarRepository _academicCalendarRepository;

        public DashboardService(
            ILogger<DashboardService> logger,
            IMongoDatabase mongoDatabase,
            ITransactionRepository transactionRepository,
            IAcademicCalendarRepository academicCalendarRepository)
        {
            _logger = logger;
            _mongoDatabase = mongoDatabase;
            _transactionRepository = transactionRepository;
            _academicCalendarRepository = academicCalendarRepository;
        }

        public async Task<DashboardStatisticsDto> GetDashboardStatisticsAsync(DateTime? from = null, DateTime? to = null)
        {
            try
            {
                var now = DateTime.UtcNow;
                var fromDate = from ?? now.Date;
                var toDate = to ?? now.Date.AddDays(1);

                var statistics = new DashboardStatisticsDto
                {
                    DailyStudents = await GetDailyStudentsAsync(now.Date),
                    AttendanceRate = await GetAttendanceRateAsync("today"),
                    VehicleRuntime = await GetVehicleRuntimeAsync(null, now.Date, now.Date.AddDays(1)),
                    RouteStatistics = await GetRouteStatisticsAsync(null, fromDate, toDate)
                };

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard statistics");
                throw;
            }
        }

        public async Task<DailyStudentsDto> GetDailyStudentsAsync(DateTime? date = null)
        {
            try
            {
                var targetDate = date ?? DateTime.UtcNow.Date;
                var tripsCollection = _mongoDatabase.GetCollection<Trip>("trips");

          
                var today = await GetStudentCountForDate(tripsCollection, targetDate);
                var yesterday = await GetStudentCountForDate(tripsCollection, targetDate.AddDays(-1));
          
                var weekStart = targetDate.AddDays(-7);
                var weekCounts = new List<int>();
                for (var d = weekStart; d < targetDate; d = d.AddDays(1))
                {
                    weekCounts.Add(await GetStudentCountForDate(tripsCollection, d));
                }
                var thisWeek = weekCounts.Any() ? (int)weekCounts.Average() : 0;

         
                var monthStart = targetDate.AddDays(-30);
                var monthCounts = new List<int>();
                for (var d = monthStart; d < targetDate; d = d.AddDays(7)) // Sample every 7 days
                {
                    monthCounts.Add(await GetStudentCountForDate(tripsCollection, d));
                }
                var thisMonth = monthCounts.Any() ? (int)monthCounts.Average() : 0;


                var last7Days = new List<DailyStudentCount>();
                for (var d = targetDate.AddDays(-6); d <= targetDate; d = d.AddDays(1))
                {
                    var count = await GetStudentCountForDate(tripsCollection, d);
                    last7Days.Add(new DailyStudentCount { Date = d, Count = count });
                }

                return new DailyStudentsDto
                {
                    Today = today,
                    Yesterday = yesterday,
                    ThisWeek = thisWeek,
                    ThisMonth = thisMonth,
                    Last7Days = last7Days
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily students");
                throw;
            }
        }

        private async Task<int> GetStudentCountForDate(IMongoCollection<Trip> collection, DateTime date)
        {
            var dayStart = date.Date;
            var dayEnd = date.Date;

            var filter = Builders<Trip>.Filter.And(
                Builders<Trip>.Filter.Eq(t => t.IsDeleted, false),
                Builders<Trip>.Filter.Gte(t => t.ServiceDate, dayStart),
                Builders<Trip>.Filter.Lte(t => t.ServiceDate, dayEnd)
            );

            var trips = await collection.Find(filter).ToListAsync();
 
            var uniqueStudents = new HashSet<Guid>();
            foreach (var trip in trips)
            {
                if (trip.Stops != null)
                {
                    foreach (var stop in trip.Stops)
                    {
                        if (stop.Attendance != null)
                        {
                            foreach (var attendance in stop.Attendance)
                            {
                                uniqueStudents.Add(attendance.StudentId);
                            }
                        }
                    }
                }
            }

            return uniqueStudents.Count;
        }

        public async Task<AttendanceRateDto> GetAttendanceRateAsync(string period = "today")
        {
            try
            {
                var now = DateTime.UtcNow;
                DateTime startDate, endDate;

                switch (period.ToLower())
                {
                    case "week":
                        startDate = now.Date.AddDays(-7);
                        endDate = now.Date.AddDays(1);
                        break;
                    case "month":
                        startDate = now.Date.AddDays(-30);
                        endDate = now.Date.AddDays(1);
                        break;
                    default: // today
                        startDate = now.Date;
                        endDate = now.Date.AddDays(1);
                        break;
                }

                var tripsCollection = _mongoDatabase.GetCollection<Trip>("trips");
                var filter = Builders<Trip>.Filter.And(
                    Builders<Trip>.Filter.Eq(t => t.IsDeleted, false),
                    Builders<Trip>.Filter.Gte(t => t.ServiceDate, startDate),
                    Builders<Trip>.Filter.Lt(t => t.ServiceDate, endDate)
                );

                var trips = await tripsCollection.Find(filter).ToListAsync();

                int totalPresent = 0, totalAbsent = 0, totalLate = 0, totalExcused = 0, totalPending = 0;

                foreach (var trip in trips)
                {
                    if (trip.Stops != null)
                    {
                        foreach (var stop in trip.Stops)
                        {
                            if (stop.Attendance != null)
                            {
                                foreach (var att in stop.Attendance)
                                {
                                    switch (att.State?.ToLower())
                                    {
                                        case "present":
                                            totalPresent++;
                                            break;
                                        case "absent":
                                            totalAbsent++;
                                            break;
                                        case "late":
                                            totalLate++;
                                            break;
                                        case "excused":
                                            totalExcused++;
                                            break;
                                        default:
                                            totalPending++;
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }

                var totalStudents = totalPresent + totalAbsent + totalLate + totalExcused + totalPending;
                var todayRate = totalStudents > 0 ? (double)totalPresent / totalStudents * 100 : 0;

                var weekRate = await CalculateAttendanceRateForPeriod(tripsCollection, now.Date.AddDays(-7), now.Date.AddDays(1));
                var monthRate = await CalculateAttendanceRateForPeriod(tripsCollection, now.Date.AddDays(-30), now.Date.AddDays(1));

                return new AttendanceRateDto
                {
                    TodayRate = Math.Round(todayRate, 2),
                    WeekRate = Math.Round(weekRate, 2),
                    MonthRate = Math.Round(monthRate, 2),
                    TotalStudents = totalStudents,
                    TotalPresent = totalPresent,
                    TotalAbsent = totalAbsent,
                    TotalLate = totalLate,
                    TotalExcused = totalExcused,
                    TotalPending = totalPending
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attendance rate for period: {Period}", period);
                throw;
            }
        }

        private async Task<double> CalculateAttendanceRateForPeriod(IMongoCollection<Trip> collection, DateTime start, DateTime end)
        {
            var filter = Builders<Trip>.Filter.And(
                Builders<Trip>.Filter.Eq(t => t.IsDeleted, false),
                Builders<Trip>.Filter.Gte(t => t.ServiceDate, start),
                Builders<Trip>.Filter.Lt(t => t.ServiceDate, end)
            );

            var trips = await collection.Find(filter).ToListAsync();
            int present = 0, total = 0;

            foreach (var trip in trips)
            {
                if (trip.Stops != null)
                {
                    foreach (var stop in trip.Stops)
                    {
                        if (stop.Attendance != null)
                        {
                            foreach (var att in stop.Attendance)
                            {
                                total++;
                                if (att.State?.ToLower() == "present")
                                    present++;
                            }
                        }
                    }
                }
            }

            return total > 0 ? (double)present / total * 100 : 0;
        }

        public async Task<VehicleRuntimeDto> GetVehicleRuntimeAsync(Guid? vehicleId = null, DateTime? from = null, DateTime? to = null)
        {
            try
            {
                var startDate = from ?? DateTime.UtcNow.Date;
                var endDate = to ?? DateTime.UtcNow.Date.AddDays(1);

                var tripsCollection = _mongoDatabase.GetCollection<Trip>("trips");
                var filterBuilder = Builders<Trip>.Filter;
                
                var filters = new List<FilterDefinition<Trip>>
                {
                    filterBuilder.Eq(t => t.IsDeleted, false),
                    filterBuilder.Gte(t => t.ServiceDate, startDate),
                    filterBuilder.Lte(t => t.ServiceDate, endDate)
                };

                if (vehicleId.HasValue)
                    filters.Add(filterBuilder.Eq(t => t.VehicleId, vehicleId.Value));

                var filter = filterBuilder.And(filters);
                var trips = await tripsCollection.Find(filter).ToListAsync();

                double totalHours = 0;
                var vehicleUsage = new Dictionary<Guid, (string plate, double hours, int count)>();

                foreach (var trip in trips)
                {
                    if (trip.StartTime.HasValue && trip.EndTime.HasValue)
                    {
                        var duration = (trip.EndTime.Value - trip.StartTime.Value).TotalHours;
                        totalHours += duration;

                        if (trip.VehicleId != Guid.Empty)
                        {
                            var plate = trip.Vehicle?.MaskedPlate ?? "Unknown";
                            if (vehicleUsage.ContainsKey(trip.VehicleId))
                            {
                                var current = vehicleUsage[trip.VehicleId];
                                vehicleUsage[trip.VehicleId] = (current.plate, current.hours + duration, current.count + 1);
                            }
                            else
                            {
                                vehicleUsage[trip.VehicleId] = (plate, duration, 1);
                            }
                        }
                    }
                }

                var topVehicles = vehicleUsage
                    .OrderByDescending(v => v.Value.hours)
                    .Take(5)
                    .Select(v => new VehicleUsage
                    {
                        VehicleId = v.Key,
                        LicensePlate = v.Value.plate,
                        TotalHours = Math.Round(v.Value.hours, 2),
                        TripCount = v.Value.count
                    })
                    .ToList();

                var totalTrips = trips.Count(t => t.StartTime.HasValue && t.EndTime.HasValue);

                return new VehicleRuntimeDto
                {
                    TotalHoursToday = Math.Round(totalHours, 2),
                    AverageHoursPerTrip = totalTrips > 0 ? Math.Round(totalHours / totalTrips, 2) : 0,
                    TotalTripsToday = totalTrips,
                    TopVehicles = topVehicles
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vehicle runtime");
                throw;
            }
        }

        public async Task<List<RouteStatisticsDto>> GetRouteStatisticsAsync(Guid? routeId = null, DateTime? from = null, DateTime? to = null)
        {
            try
            {
                var startDate = from ?? DateTime.UtcNow.Date.AddDays(-30);
                var endDate = to ?? DateTime.UtcNow.Date.AddDays(1);

                var tripsCollection = _mongoDatabase.GetCollection<Trip>("trips");
                var routesCollection = _mongoDatabase.GetCollection<Route>("routes");

                var filterBuilder = Builders<Trip>.Filter;
                var filters = new List<FilterDefinition<Trip>>
                {
                    filterBuilder.Eq(t => t.IsDeleted, false),
                    filterBuilder.Gte(t => t.ServiceDate, startDate),
                    filterBuilder.Lte(t => t.ServiceDate, endDate)
                };

                if (routeId.HasValue)
                    filters.Add(filterBuilder.Eq(t => t.RouteId, routeId.Value));

                var filter = filterBuilder.And(filters);
                var trips = await tripsCollection.Find(filter).ToListAsync();

                var routeStats = new Dictionary<Guid, (string name, int tripCount, HashSet<Guid> students, int present, int total, double totalRuntime, HashSet<Guid> vehicles)>();

                foreach (var trip in trips)
                {
                    if (!routeStats.ContainsKey(trip.RouteId))
                    {
                        var route = await routesCollection.Find(r => r.Id == trip.RouteId).FirstOrDefaultAsync();
                        var routeName = route?.RouteName ?? "Unknown Route";
                        routeStats[trip.RouteId] = (routeName, 0, new HashSet<Guid>(), 0, 0, 0, new HashSet<Guid>());
                    }

                    var current = routeStats[trip.RouteId];
                    current.tripCount++;

                    if (trip.VehicleId != Guid.Empty)
                        current.vehicles.Add(trip.VehicleId);

                    if (trip.StartTime.HasValue && trip.EndTime.HasValue)
                    {
                        current.totalRuntime += (trip.EndTime.Value - trip.StartTime.Value).TotalHours;
                    }

                    if (trip.Stops != null)
                    {
                        foreach (var stop in trip.Stops)
                        {
                            if (stop.Attendance != null)
                            {
                                foreach (var att in stop.Attendance)
                                {
                                    current.students.Add(att.StudentId);
                                    current.total++;
                                    if (att.State?.ToLower() == "present")
                                        current.present++;
                                }
                            }
                        }
                    }

                    routeStats[trip.RouteId] = current;
                }

                return routeStats.Select(r => new RouteStatisticsDto
                {
                    RouteId = r.Key,
                    RouteName = r.Value.name,
                    TotalTrips = r.Value.tripCount,
                    TotalStudents = r.Value.students.Count,
                    AttendanceRate = r.Value.total > 0 ? Math.Round((double)r.Value.present / r.Value.total * 100, 2) : 0,
                    AverageRuntime = r.Value.tripCount > 0 ? Math.Round(r.Value.totalRuntime / r.Value.tripCount, 2) : 0,
                    ActiveVehicles = r.Value.vehicles.Count
                }).OrderByDescending(r => r.TotalTrips).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting route statistics");
                throw;
            }
        }

        public async Task<RevenueStatisticsDto> GetRevenueStatisticsAsync(DateTime? from = null, DateTime? to = null)
        {
            try
            {
                var startDate = from ?? DateTime.UtcNow.Date.AddMonths(-1);
                var endDate = to ?? DateTime.UtcNow;

                var transactionsQuery = _transactionRepository.GetQueryable().Where(t => !t.IsDeleted);

                transactionsQuery = transactionsQuery.Where(t =>
                    (t.PaidAtUtc ?? t.CreatedAt) >= startDate &&
                    (t.PaidAtUtc ?? t.CreatedAt) <= endDate);

                var transactions = await transactionsQuery.ToListAsync();

                var paidTransactions = transactions.Where(t => t.Status == TransactionStatus.Paid).ToList();
                var pendingTransactions = transactions.Where(t => t.Status == TransactionStatus.Notyet).ToList();
                var failedTransactions = transactions.Where(t =>
                    t.Status == TransactionStatus.Failed ||
                    t.Status == TransactionStatus.Cancelled ||
                    t.Status == TransactionStatus.Expired).ToList();

                return new RevenueStatisticsDto
                {
                    TotalRevenue = paidTransactions.Sum(t => t.Amount),
                    PendingAmount = pendingTransactions.Sum(t => t.Amount),
                    FailedAmount = failedTransactions.Sum(t => t.Amount),
                    PaidTransactionCount = paidTransactions.Count,
                    PendingTransactionCount = pendingTransactions.Count,
                    FailedTransactionCount = failedTransactions.Count,
                    Currency = transactions.FirstOrDefault()?.Currency ?? "VND"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting revenue statistics");
                throw;
            }
        }

        public async Task<List<RevenueTimelinePointDto>> GetRevenueTimelineAsync(DateTime? from = null, DateTime? to = null)
        {
            try
            {
                var startDate = from ?? DateTime.UtcNow.Date.AddMonths(-1);
                var endDate = to ?? DateTime.UtcNow.Date.AddDays(1);

                var transactionsQuery = _transactionRepository.GetQueryable().Where(t =>
                    !t.IsDeleted &&
                    t.Status == TransactionStatus.Paid &&
                    (t.PaidAtUtc ?? t.CreatedAt) >= startDate &&
                    (t.PaidAtUtc ?? t.CreatedAt) < endDate);

                var timeline = await transactionsQuery
                    .GroupBy(t => (t.PaidAtUtc ?? t.CreatedAt).Date)
                    .Select(g => new RevenueTimelinePointDto
                    {
                        Date = g.Key,
                        Amount = g.Sum(t => t.Amount),
                        Count = g.Count()
                    })
                    .OrderBy(p => p.Date)
                    .ToListAsync();

                return timeline;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting revenue timeline");
                throw;
            }
        }

        public async Task<ActiveSemesterDto?> GetCurrentSemesterAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var activeCalendars = await _academicCalendarRepository.GetActiveAsync();
                if (!activeCalendars.Any())
                {
                    return null;
                }

                var currentSemester = activeCalendars
                    .SelectMany(cal => cal.Semesters
                        .Where(s => s.IsActive && s.StartDate <= now && s.EndDate >= now)
                        .Select(s => new { Calendar = cal, Semester = s }))
                    .OrderBy(x => x.Semester.StartDate)
                    .FirstOrDefault();

                if (currentSemester == null)
                {
                    // If no current semester, pick the next upcoming active semester
                    currentSemester = activeCalendars
                        .SelectMany(cal => cal.Semesters
                            .Where(s => s.IsActive && s.StartDate > now)
                            .Select(s => new { Calendar = cal, Semester = s }))
                        .OrderBy(x => x.Semester.StartDate)
                        .FirstOrDefault();
                }

                if (currentSemester == null)
                {
                    // Fallback to the latest past active semester
                    currentSemester = activeCalendars
                        .SelectMany(cal => cal.Semesters
                            .Where(s => s.IsActive && s.EndDate < now)
                            .Select(s => new { Calendar = cal, Semester = s }))
                        .OrderByDescending(x => x.Semester.EndDate)
                        .FirstOrDefault();
                }

                if (currentSemester == null)
                {
                    return null;
                }

                _logger.LogInformation("Querying enrollment settings for SemesterCode: {Code}, AcademicYear: {Year}", 
                    currentSemester.Semester.Code, currentSemester.Calendar.AcademicYear);

                var enrollmentSettings = await _mongoDatabase.GetCollection<EnrollmentSemesterSettings>("EnrollmentSemesterSettings")
                    .Find(e => e.SemesterCode == currentSemester.Semester.Code 
                            && e.AcademicYear == currentSemester.Calendar.AcademicYear
                            && !e.IsDeleted)
                    .FirstOrDefaultAsync();

                if (enrollmentSettings == null)
                {
                    _logger.LogWarning("No enrollment settings found for SemesterCode: {Code}, AcademicYear: {Year}", 
                        currentSemester.Semester.Code, currentSemester.Calendar.AcademicYear);
                }
                else
                {
                    _logger.LogInformation("Found enrollment settings with registration dates: {Start} to {End}",
                        enrollmentSettings.RegistrationStartDate, enrollmentSettings.RegistrationEndDate);
                }

                return new ActiveSemesterDto
                {
                    AcademicYear = currentSemester.Calendar.AcademicYear,
                    SemesterCode = currentSemester.Semester.Code,
                    SemesterName = currentSemester.Semester.Name,
                    SemesterStartDate = currentSemester.Semester.StartDate,
                    SemesterEndDate = currentSemester.Semester.EndDate,
                    RegistrationStartDate = enrollmentSettings?.RegistrationStartDate,
                    RegistrationEndDate = enrollmentSettings?.RegistrationEndDate
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current semester");
                throw;
            }
        }

        public async Task<List<ActiveSemesterDto>> GetAllSemestersAsync()
        {
            try
            {
                var activeCalendars = await _academicCalendarRepository.GetActiveAsync();
                if (!activeCalendars.Any())
                {
                    return new List<ActiveSemesterDto>();
                }

                var semesters = activeCalendars
                    .SelectMany(cal => cal.Semesters
                        .Where(s => s.IsActive)
                        .Select(s => new { Calendar = cal, Semester = s }))
                    .ToList();

                var semesterCodes = semesters.Select(s => s.Semester.Code).ToList();
                var enrollmentSettingsCollection = _mongoDatabase.GetCollection<EnrollmentSemesterSettings>("EnrollmentSemesterSettings");
                var enrollmentSettingsList = await enrollmentSettingsCollection
                    .Find(e => semesterCodes.Contains(e.SemesterCode) && !e.IsDeleted)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} enrollment settings records for {CodesCount} semester codes",
                    enrollmentSettingsList.Count, semesterCodes.Count);

                var result = semesters
                    .Select(s =>
                    {
                        var enrollment = enrollmentSettingsList.FirstOrDefault(e => 
                            e.SemesterCode == s.Semester.Code && 
                            e.AcademicYear == s.Calendar.AcademicYear);

                        if (enrollment != null)
                        {
                            _logger.LogInformation("Matched enrollment for {Code}-{Year}: Reg dates {Start} to {End}",
                                s.Semester.Code, s.Calendar.AcademicYear, 
                                enrollment.RegistrationStartDate, enrollment.RegistrationEndDate);
                        }
                        else
                        {
                            _logger.LogWarning("No enrollment match for {Code}-{Year}",
                                s.Semester.Code, s.Calendar.AcademicYear);
                        }

                        return new ActiveSemesterDto
                        {
                            AcademicYear = s.Calendar.AcademicYear,
                            SemesterCode = s.Semester.Code,
                            SemesterName = s.Semester.Name,
                            SemesterStartDate = s.Semester.StartDate,
                            SemesterEndDate = s.Semester.EndDate,
                            RegistrationStartDate = enrollment?.RegistrationStartDate,
                            RegistrationEndDate = enrollment?.RegistrationEndDate
                        };
                    })
                    .OrderByDescending(s => s.SemesterStartDate)
                    .ToList();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all semesters");
                throw;
            }
        }
    }
}
