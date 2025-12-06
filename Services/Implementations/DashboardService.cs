using Constants;
using Data.Models;
using Data.Repos.Interfaces;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Services.Contracts;
using Services.Models.Dashboard;
using Route = Data.Models.Route;
using Utils;

namespace Services.Implementations
{
    public class DashboardService : IDashboardService
    {
        private readonly IDatabaseFactory _databaseFactory;
        private readonly ILogger<DashboardService> _logger;
        private readonly IMongoDatabase _mongoDatabase;

        public DashboardService(
            IDatabaseFactory databaseFactory,
            ILogger<DashboardService> logger,
            IMongoDatabase mongoDatabase)
        {
            _databaseFactory = databaseFactory;
            _logger = logger;
            _mongoDatabase = mongoDatabase;
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

                // Get counts for different periods
                var today = await GetStudentCountForDate(tripsCollection, targetDate);
                var yesterday = await GetStudentCountForDate(tripsCollection, targetDate.AddDays(-1));
                
                // Week average
                var weekStart = targetDate.AddDays(-7);
                var weekCounts = new List<int>();
                for (var d = weekStart; d < targetDate; d = d.AddDays(1))
                {
                    weekCounts.Add(await GetStudentCountForDate(tripsCollection, d));
                }
                var thisWeek = weekCounts.Any() ? (int)weekCounts.Average() : 0;

                // Month average
                var monthStart = targetDate.AddDays(-30);
                var monthCounts = new List<int>();
                for (var d = monthStart; d < targetDate; d = d.AddDays(7)) // Sample every 7 days
                {
                    monthCounts.Add(await GetStudentCountForDate(tripsCollection, d));
                }
                var thisMonth = monthCounts.Any() ? (int)monthCounts.Average() : 0;

                // Last 7 days detail
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
            var dayEnd = dayStart.AddDays(1);

            var filter = Builders<Trip>.Filter.And(
                Builders<Trip>.Filter.Eq(t => t.IsDeleted, false),
                Builders<Trip>.Filter.Gte(t => t.ServiceDate, dayStart),
                Builders<Trip>.Filter.Lt(t => t.ServiceDate, dayEnd)
            );

            var trips = await collection.Find(filter).ToListAsync();
            
            // Count unique students across all trips
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

                // Calculate week and month rates
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
                    filterBuilder.Lt(t => t.ServiceDate, endDate)
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
                    filterBuilder.Lt(t => t.ServiceDate, endDate)
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
    }
}
