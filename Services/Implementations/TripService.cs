using Data.Models;
using Data.Repos.Interfaces;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Services.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace Services.Implementations
{
	public class TripService : ITripService
	{
		private readonly IDatabaseFactory _databaseFactory;
		private readonly ILogger<TripService> _logger;

		public TripService(IDatabaseFactory databaseFactory, ILogger<TripService> logger)
		{
			_databaseFactory = databaseFactory;
			_logger = logger;
		}

		public async Task<IEnumerable<Trip>> QueryTripsAsync(
	Guid? routeId,
	DateTime? serviceDate,
	DateTime? startDate,
	DateTime? endDate,
	string? status,
	int page,
	int perPage,
	string sortBy,
	string sortOrder)
		{
			try
			{
				if (page < 1) page = 1;
				if (perPage < 1) perPage = 20;

				var repository = _databaseFactory.GetRepositoryByType<ITripRepository>(DatabaseType.MongoDb);

				var filters = new List<FilterDefinition<Trip>>
		{
			Builders<Trip>.Filter.Eq(t => t.IsDeleted, false)
		};

				if (routeId.HasValue)
					filters.Add(Builders<Trip>.Filter.Eq(t => t.RouteId, routeId.Value));

				if (!string.IsNullOrWhiteSpace(status))
					filters.Add(Builders<Trip>.Filter.Eq(t => t.Status, status));

				if (serviceDate.HasValue)
				{
					var dayStart = serviceDate.Value.Date;
					var dayEnd = dayStart.AddDays(1);
					filters.Add(Builders<Trip>.Filter.Gte(t => t.ServiceDate, dayStart));
					filters.Add(Builders<Trip>.Filter.Lt(t => t.ServiceDate, dayEnd));
				}
				else if (startDate.HasValue && endDate.HasValue)
				{
					filters.Add(Builders<Trip>.Filter.Gte(t => t.ServiceDate, startDate.Value.Date));
					filters.Add(Builders<Trip>.Filter.Lte(t => t.ServiceDate, endDate.Value.Date));
				}

				var filter = filters.Count == 1 ? filters[0] : Builders<Trip>.Filter.And(filters);

				var desc = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
				SortDefinition<Trip> sort = sortBy?.ToLowerInvariant() switch
				{
					"plannedstartat" => desc ? Builders<Trip>.Sort.Descending(x => x.PlannedStartAt) : Builders<Trip>.Sort.Ascending(x => x.PlannedStartAt),
					"plannedendat" => desc ? Builders<Trip>.Sort.Descending(x => x.PlannedEndAt) : Builders<Trip>.Sort.Ascending(x => x.PlannedEndAt),
					"status" => desc ? Builders<Trip>.Sort.Descending(x => x.Status) : Builders<Trip>.Sort.Ascending(x => x.Status),
					"servicedate" or _ => desc ? Builders<Trip>.Sort.Descending(x => x.ServiceDate) : Builders<Trip>.Sort.Ascending(x => x.ServiceDate),
				};

				var skip = (page - 1) * perPage;
				return await repository.FindByFilterAsync(filter, sort, skip, perPage);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error querying trips with pagination/sorting");
				throw;
			}
		}

		public async Task<IEnumerable<Trip>> GetAllTripsAsync()
		{
			try
			{
				var repository = _databaseFactory.GetRepositoryByType<ITripRepository>(DatabaseType.MongoDb);
				return await repository.FindAllAsync();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting all trips");
				throw;
			}
		}

		public async Task<Trip?> GetTripByIdAsync(Guid id)
		{
			try
			{
				var repository = _databaseFactory.GetRepositoryByType<ITripRepository>(DatabaseType.MongoDb);
				return await repository.FindAsync(id);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting trip with id: {TripId}", id);
				throw;
			}
		}

		public async Task<Trip> CreateTripAsync(Trip trip)
		{
			try
			{
				if (trip.RouteId == Guid.Empty)
					throw new ArgumentException("Route ID is required");

				if (trip.PlannedStartAt >= trip.PlannedEndAt)
					throw new ArgumentException("Planned start time must be before planned end time");

				var repository = _databaseFactory.GetRepositoryByType<ITripRepository>(DatabaseType.MongoDb);
				return await repository.AddAsync(trip);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating trip: {@Trip}", trip);
				throw;
			}
		}

		public async Task<Trip?> UpdateTripAsync(Trip trip)
		{
			try
			{
				var repository = _databaseFactory.GetRepositoryByType<ITripRepository>(DatabaseType.MongoDb);
				var existingTrip = await repository.FindAsync(trip.Id);
				if (existingTrip == null)
					return null;

				return await repository.UpdateAsync(trip);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating trip: {@Trip}", trip);
				throw;
			}
		}

		public async Task<Trip?> DeleteTripAsync(Guid id)
		{
			try
			{
				var repository = _databaseFactory.GetRepositoryByType<ITripRepository>(DatabaseType.MongoDb);
				return await repository.DeleteAsync(id);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting trip with id: {TripId}", id);
				throw;
			}
		}

		public async Task<IEnumerable<Trip>> GetTripsByRouteAsync(Guid routeId)
		{
			try
			{
				var repository = _databaseFactory.GetRepositoryByType<ITripRepository>(DatabaseType.MongoDb);
				return await repository.GetTripsByRouteAsync(routeId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting trips by route: {RouteId}", routeId);
				throw;
			}
		}

		public async Task<IEnumerable<Trip>> GetTripsByDateAsync(DateTime serviceDate)
		{
			try
			{
				var repository = _databaseFactory.GetRepositoryByType<ITripRepository>(DatabaseType.MongoDb);
				return await repository.GetTripsByDateAsync(serviceDate);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting trips by date: {ServiceDate}", serviceDate);
				throw;
			}
		}

		public async Task<IEnumerable<Trip>> GetTripsByDateRangeAsync(DateTime startDate, DateTime endDate)
		{
			try
			{
				var repository = _databaseFactory.GetRepositoryByType<ITripRepository>(DatabaseType.MongoDb);
				return await repository.GetTripsByDateRangeAsync(startDate, endDate);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting trips by date range: {StartDate} to {EndDate}", startDate, endDate);
				throw;
			}
		}

		public async Task<IEnumerable<Trip>> GetTripsByStatusAsync(string status)
		{
			try
			{
				var repository = _databaseFactory.GetRepositoryByType<ITripRepository>(DatabaseType.MongoDb);
				return await repository.GetTripsByStatusAsync(status);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting trips by status: {Status}", status);
				throw;
			}
		}

		public async Task<IEnumerable<Trip>> GetUpcomingTripsAsync(DateTime fromDate, int days = 7)
		{
			try
			{
				var repository = _databaseFactory.GetRepositoryByType<ITripRepository>(DatabaseType.MongoDb);
				return await repository.GetUpcomingTripsAsync(fromDate, days);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting upcoming trips from: {FromDate} for {Days} days", fromDate, days);
				throw;
			}
		}

		public async Task<bool> TripExistsAsync(Guid id)
		{
			try
			{
				var repository = _databaseFactory.GetRepositoryByType<ITripRepository>(DatabaseType.MongoDb);
				return await repository.ExistsAsync(id);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error checking if trip exists: {TripId}", id);
				throw;
			}
		}

		public async Task<IEnumerable<Trip>> GenerateTripsFromScheduleAsync(Guid scheduleId, DateTime startDate, DateTime endDate)
		{
			try
			{
				if (endDate <= startDate)
					throw new ArgumentException("endDate must be greater than startDate");

				var scheduleRepo = _databaseFactory.GetRepositoryByType<IScheduleRepository>(DatabaseType.MongoDb);
				var tripRepo = _databaseFactory.GetRepositoryByType<ITripRepository>(DatabaseType.MongoDb);
				var routeScheduleRepo = _databaseFactory.GetRepositoryByType<IRouteScheduleRepository>(DatabaseType.MongoDb);

				var schedule = await scheduleRepo.FindAsync(scheduleId);
				if (schedule == null)
					throw new ArgumentException("Schedule not found");

				if (!schedule.IsActive)
					return Enumerable.Empty<Trip>();

				// limit to schedule effective window
				var windowStart = startDate.Date < schedule.EffectiveFrom.Date ? schedule.EffectiveFrom.Date : startDate.Date;
				var windowEnd = schedule.EffectiveTo.HasValue && endDate.Date > schedule.EffectiveTo.Value.Date
					? schedule.EffectiveTo.Value.Date
					: endDate.Date;

				if (windowEnd < windowStart)
					return Enumerable.Empty<Trip>();

				// load all active route-schedule links for this schedule in the window (coarse filter)
				var routeLinks = (await routeScheduleRepo.GetRouteSchedulesByScheduleAsync(scheduleId))
					.Where(rs => rs.IsActive)
					.ToList();

				if (routeLinks.Count == 0)
					return Enumerable.Empty<Trip>();

				// timezone
				TimeZoneInfo tz;
				try { tz = TimeZoneInfo.FindSystemTimeZoneById(schedule.Timezone); }
				catch { tz = TimeZoneInfo.Utc; }

				var (freq, byDays) = ParseBasicRRule(schedule.RRule);
				var created = new List<Trip>();

				for (var date = windowStart; date <= windowEnd; date = date.AddDays(1))
				{
					// exceptions
					if (schedule.Exceptions != null && schedule.Exceptions.Any(ex => ex.Date == date))
						continue;

					// RRULE filter
					if (freq == "WEEKLY" && byDays.Count > 0)
					{
						var iso = DayOfWeekToIcs(date.DayOfWeek);
						if (!byDays.Contains(iso)) continue;
					}

					// find active route-links for this specific date; pick highest priority per route if multiple
					var activeLinksForDate = routeLinks
						.Where(rs =>
							rs.EffectiveFrom.Date <= date &&
							(!rs.EffectiveTo.HasValue || rs.EffectiveTo.Value.Date >= date))
						.GroupBy(rs => rs.RouteId)
						.Select(g => g.OrderByDescending(x => x.Priority).First())
						.ToList();

					if (activeLinksForDate.Count == 0)
						continue;

					// parse times
					if (!TryParseHms(schedule.StartTime, out var sh, out var sm, out var ss) ||
						!TryParseHms(schedule.EndTime, out var eh, out var em, out var es))
						continue;

					var localStart = new DateTime(date.Year, date.Month, date.Day, sh, sm, ss, DateTimeKind.Unspecified);
					var localEnd = new DateTime(date.Year, date.Month, date.Day, eh, em, es, DateTimeKind.Unspecified);
					if (localEnd <= localStart) localEnd = localEnd.AddDays(1);

					var utcStart = TimeZoneInfo.ConvertTimeToUtc(localStart, tz);
					var utcEnd = TimeZoneInfo.ConvertTimeToUtc(localEnd, tz);

					// generate for each active route
					foreach (var link in activeLinksForDate)
					{
						// idempotency: same routeId + serviceDate + plannedStartAt
						var existing = await tripRepo.FindByFilterAsync(
							Builders<Trip>.Filter.And(
								Builders<Trip>.Filter.Eq(t => t.RouteId, link.RouteId),
								Builders<Trip>.Filter.Eq(t => t.ServiceDate, date),
								Builders<Trip>.Filter.Eq(t => t.PlannedStartAt, utcStart),
								Builders<Trip>.Filter.Eq(t => t.IsDeleted, false)
							)
						);
						if (existing.Any())
							continue;

						var trip = new Trip
						{
							RouteId = link.RouteId,
							ServiceDate = date,
							PlannedStartAt = utcStart,
							PlannedEndAt = utcEnd,
							Status = "Scheduled",
							ScheduleSnapshot = new ScheduleSnapshot
							{
								ScheduleId = schedule.Id,
								Name = schedule.Name,
								StartTime = schedule.StartTime,
								EndTime = schedule.EndTime,
								RRule = schedule.RRule
							},
							Stops = new List<TripStop>()
						};

						trip = await tripRepo.AddAsync(trip);
						created.Add(trip);
					}
				}

				return created;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error generating trips from schedule: {ScheduleId}", scheduleId);
				throw;
			}
		}

		private static bool TryParseHms(string hms, out int h, out int m, out int s)
		{
			h = m = s = 0;
			if (string.IsNullOrWhiteSpace(hms)) return false;
			var parts = hms.Split(':');
			if (parts.Length < 2) return false;
			if (!int.TryParse(parts[0], out h)) return false;
			if (!int.TryParse(parts[1], out m)) return false;
			if (parts.Length >= 3) int.TryParse(parts[2], out s);
			return h >= 0 && h <= 23 && m >= 0 && m <= 59 && s >= 0 && s <= 59;
		}

		private static (string freq, HashSet<string> byDays) ParseBasicRRule(string rrule)
		{
			var freq = "DAILY";
			var byDays = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			if (string.IsNullOrWhiteSpace(rrule)) return (freq, byDays);

			var parts = rrule.Split(';', StringSplitOptions.RemoveEmptyEntries);
			foreach (var p in parts)
			{
				var kv = p.Split('=', 2);
				if (kv.Length != 2) continue;
				var key = kv[0].Trim().ToUpperInvariant();
				var val = kv[1].Trim().ToUpperInvariant();

				if (key == "FREQ")
				{
					if (val == "DAILY" || val == "WEEKLY") freq = val;
				}
				else if (key == "BYDAY")
				{
					foreach (var d in val.Split(',', StringSplitOptions.RemoveEmptyEntries))
						byDays.Add(d.Trim());
				}
			}
			return (freq, byDays);
		}

		private static string DayOfWeekToIcs(DayOfWeek dow)
		{
			return dow switch
			{
				DayOfWeek.Monday => "MO",
				DayOfWeek.Tuesday => "TU",
				DayOfWeek.Wednesday => "WE",
				DayOfWeek.Thursday => "TH",
				DayOfWeek.Friday => "FR",
				DayOfWeek.Saturday => "SA",
				DayOfWeek.Sunday => "SU",
				_ => "MO"
			};
		}
	}
}
