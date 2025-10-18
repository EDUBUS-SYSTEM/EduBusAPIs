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
using Constants;

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
			await ValidateTripCreationAsync(trip);

			await GenerateTripStopsFromRouteAsync(trip);

			trip.Status = TripStatus.Scheduled;

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

			if (existingTrip.Status != trip.Status)
			{
				if (!TripStatusTransitions.IsValidTransition(existingTrip.Status, trip.Status))
					throw new InvalidOperationException($"Invalid status transition from {existingTrip.Status} to {trip.Status}");
			}

			await ValidateTripUpdateAsync(trip);

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

				var timeOverride = schedule.TimeOverrides?.FirstOrDefault(o => o.Date.Date == date.Date);
					
					// Skip if override is cancelled
					if (timeOverride?.IsCancelled == true)
						continue;

					// Use override times if available, otherwise use schedule times
					var startTime = timeOverride?.StartTime ?? schedule.StartTime;
					var endTime = timeOverride?.EndTime ?? schedule.EndTime;

					// parse times
					if (!TryParseHms(startTime, out var sh, out var sm, out var ss) ||
						!TryParseHms(endTime, out var eh, out var em, out var es))
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

					if (timeOverride != null)
						{
							trip.IsOverride = true;
							trip.OverrideReason = timeOverride.Reason;
							trip.OverrideCreatedBy = timeOverride.CreatedBy;
							trip.OverrideCreatedAt = timeOverride.CreatedAt;
							trip.OverrideInfo = new OverrideInfo
							{
								ScheduleId = schedule.Id.ToString(),
								OverrideType = "TIME_CHANGE",
								OriginalStartTime = schedule.StartTime,
								OriginalEndTime = schedule.EndTime,
								NewStartTime = timeOverride.StartTime,
								NewEndTime = timeOverride.EndTime,
								OverrideReason = timeOverride.Reason,
								OverrideCreatedAt = timeOverride.CreatedAt,
								OverrideCreatedBy = timeOverride.CreatedBy
							};
						}

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

		private async Task ValidateTripCreationAsync(Trip trip)
		{
			// Basic validation
			if (trip.RouteId == Guid.Empty)
				throw new ArgumentException("Route ID is required");

			if (trip.PlannedStartAt >= trip.PlannedEndAt)
				throw new ArgumentException("Planned start time must be before planned end time");

			if (trip.ServiceDate.Date < DateTime.UtcNow.Date)
				throw new ArgumentException("Service date cannot be in the past");

			// Validate Route exists and is active
			var routeRepo = _databaseFactory.GetRepositoryByType<IMongoRepository<Route>>(DatabaseType.MongoDb);
			var route = await routeRepo.FindAsync(trip.RouteId);
			if (route == null || route.IsDeleted || !route.IsActive)
				throw new InvalidOperationException($"Route {trip.RouteId} does not exist or is inactive");

			// Validate ScheduleSnapshot if provided
			if (trip.ScheduleSnapshot != null && trip.ScheduleSnapshot.ScheduleId != Guid.Empty)
			{
				var scheduleRepo = _databaseFactory.GetRepositoryByType<IMongoRepository<Schedule>>(DatabaseType.MongoDb);
				var schedule = await scheduleRepo.FindAsync(trip.ScheduleSnapshot.ScheduleId);
				if (schedule == null || schedule.IsDeleted || !schedule.IsActive)
					throw new InvalidOperationException($"Schedule {trip.ScheduleSnapshot.ScheduleId} does not exist or is inactive");
			}

			// Check for duplicate trips
			var tripRepo = _databaseFactory.GetRepositoryByType<ITripRepository>(DatabaseType.MongoDb);
			var existingTrips = await tripRepo.FindByFilterAsync(
				Builders<Trip>.Filter.And(
					Builders<Trip>.Filter.Eq(t => t.RouteId, trip.RouteId),
					Builders<Trip>.Filter.Eq(t => t.ServiceDate, trip.ServiceDate),
					Builders<Trip>.Filter.Eq(t => t.PlannedStartAt, trip.PlannedStartAt),
					Builders<Trip>.Filter.Eq(t => t.IsDeleted, false)
				)
			);

			if (existingTrips.Any())
				throw new InvalidOperationException("A trip with the same route, date, and start time already exists");
		}

		private async Task ValidateTripUpdateAsync(Trip trip)
		{
			// Basic validation
			if (trip.RouteId == Guid.Empty)
				throw new ArgumentException("Route ID is required");

			if (trip.PlannedStartAt >= trip.PlannedEndAt)
				throw new ArgumentException("Planned start time must be before planned end time");

			// Validate Route exists and is active
			var routeRepo = _databaseFactory.GetRepositoryByType<IMongoRepository<Route>>(DatabaseType.MongoDb);
			var route = await routeRepo.FindAsync(trip.RouteId);
			if (route == null || route.IsDeleted || !route.IsActive)
				throw new InvalidOperationException($"Route {trip.RouteId} does not exist or is inactive");
		}

		private async Task GenerateTripStopsFromRouteAsync(Trip trip)
		{
			if (trip.Stops.Any())
				return; // Already has stops

			var routeRepo = _databaseFactory.GetRepositoryByType<IMongoRepository<Route>>(DatabaseType.MongoDb);
			var route = await routeRepo.FindAsync(trip.RouteId);
			if (route == null)
				throw new InvalidOperationException($"Route {trip.RouteId} not found");

			if (!route.PickupPoints.Any())
			{
				_logger.LogWarning("Route {RouteId} has no pickup points", trip.RouteId);
				return;
			}

			// Generate TripStops from Route pickup points
			trip.Stops = new List<TripStop>();
			var totalStops = route.PickupPoints.Count;
			var timePerStop = TimeSpan.FromMinutes(5); // Default 5 minutes per stop

			for (int i = 0; i < totalStops; i++)
			{
				var pickupPoint = route.PickupPoints[i];
				var plannedAt = trip.PlannedStartAt.Add(timePerStop * i);

				var tripStop = new TripStop
				{
					SequenceOrder = i + 1,
					PickupPointId = pickupPoint.PickupPointId,
					PlannedAt = plannedAt,
					Location = new LocationInfo
					{
						Latitude = pickupPoint.Location.Latitude,
						Longitude = pickupPoint.Location.Longitude,
						Address = pickupPoint.Location.Address
					},
					Attendance = new List<Attendance>()
				};

				trip.Stops.Add(tripStop);
			}

			_logger.LogInformation("Generated {Count} trip stops for trip {TripId}", trip.Stops.Count, trip.Id);
		}

		public async Task<IEnumerable<Trip>> RegenerateTripsForDateAsync(Guid scheduleId, DateTime date)
		{
			try
			{
				var scheduleRepo = _databaseFactory.GetRepositoryByType<IScheduleRepository>(DatabaseType.MongoDb);
				var tripRepo = _databaseFactory.GetRepositoryByType<ITripRepository>(DatabaseType.MongoDb);
				var routeScheduleRepo = _databaseFactory.GetRepositoryByType<IRouteScheduleRepository>(DatabaseType.MongoDb);

				var schedule = await scheduleRepo.FindAsync(scheduleId);
				if (schedule == null)
					throw new ArgumentException("Schedule not found");

				// Get active route schedules for this schedule
				var routeLinks = (await routeScheduleRepo.GetRouteSchedulesByScheduleAsync(scheduleId))
					.Where(rs => rs.IsActive && 
						rs.EffectiveFrom.Date <= date &&
						(!rs.EffectiveTo.HasValue || rs.EffectiveTo.Value.Date >= date))
					.GroupBy(rs => rs.RouteId)
					.Select(g => g.OrderByDescending(x => x.Priority).First())
					.ToList();

				if (!routeLinks.Any())
					return Enumerable.Empty<Trip>();

				// Delete existing trips for this date and schedule
				var existingTrips = await tripRepo.FindByFilterAsync(
					Builders<Trip>.Filter.And(
						Builders<Trip>.Filter.Eq(t => t.ServiceDate, date),
						Builders<Trip>.Filter.Eq(t => t.ScheduleSnapshot.ScheduleId, scheduleId),
						Builders<Trip>.Filter.Eq(t => t.IsDeleted, false)
					)
				);

				foreach (var trip in existingTrips)
				{
					await tripRepo.DeleteAsync(trip.Id);
				}

				// Regenerate trips for this specific date
				var regeneratedTrips = await GenerateTripsFromScheduleAsync(scheduleId, date, date);
				
				_logger.LogInformation("Regenerated {Count} trips for schedule {ScheduleId} on {Date}", 
					regeneratedTrips.Count(), scheduleId, date);

				return regeneratedTrips;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error regenerating trips for schedule {ScheduleId} on {Date}", scheduleId, date);
				throw;
			}
		}

		public async Task<IEnumerable<Trip>> GetTripsAffectedByScheduleOverrideAsync(Guid scheduleId, DateTime date)
		{
			try
			{
				var tripRepo = _databaseFactory.GetRepositoryByType<ITripRepository>(DatabaseType.MongoDb);
				
				var trips = await tripRepo.FindByFilterAsync(
					Builders<Trip>.Filter.And(
						Builders<Trip>.Filter.Eq(t => t.ServiceDate, date),
						Builders<Trip>.Filter.Eq(t => t.ScheduleSnapshot.ScheduleId, scheduleId),
						Builders<Trip>.Filter.Eq(t => t.IsDeleted, false)
					)
				);

				return trips;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting trips affected by schedule override: {ScheduleId} on {Date}", scheduleId, date);
				throw;
			}
		}

		public async Task<bool> UpdateTripStatusAsync(Guid tripId, string newStatus, string? reason = null)
		{
			try
			{
				var repository = _databaseFactory.GetRepositoryByType<ITripRepository>(DatabaseType.MongoDb);
				var trip = await repository.FindAsync(tripId);
				if (trip == null)
					return false;

				// Validate status transition
				if (!TripStatusTransitions.IsValidTransition(trip.Status, newStatus))
					throw new InvalidOperationException($"Invalid status transition from {trip.Status} to {newStatus}");

				// Update status
				trip.Status = newStatus;
				trip.UpdatedAt = DateTime.UtcNow;

				// Set actual times based on status
				if (newStatus == TripStatus.InProgress && !trip.StartTime.HasValue)
				{
					trip.StartTime = DateTime.UtcNow;
				}
				else if (newStatus == TripStatus.Completed && !trip.EndTime.HasValue)
				{
					trip.EndTime = DateTime.UtcNow;
				}

				await repository.UpdateAsync(trip);
				_logger.LogInformation("Updated trip {TripId} status from {OldStatus} to {NewStatus}", tripId, trip.Status, newStatus);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating trip status: {TripId} to {Status}", tripId, newStatus);
				throw;
			}
		}

		public async Task<bool> UpdateAttendanceAsync(Guid tripId, Guid stopId, Guid studentId, string state)
		{
			try
			{
				var repository = _databaseFactory.GetRepositoryByType<ITripRepository>(DatabaseType.MongoDb);
				var trip = await repository.FindAsync(tripId);
				if (trip == null)
					return false;

				var stop = trip.Stops.FirstOrDefault(s => s.PickupPointId == stopId);
				if (stop == null)
					return false;

				// Find existing attendance or create new
				var attendance = stop.Attendance.FirstOrDefault(a => a.StudentId == studentId);
				if (attendance == null)
				{
					attendance = new Attendance
					{
						StudentId = studentId,
						State = state,
						BoardedAt = state == AttendanceStates.Present ? DateTime.UtcNow : null
					};
					stop.Attendance.Add(attendance);
				}
				else
				{
					attendance.State = state;
					attendance.BoardedAt = state == AttendanceStates.Present ? DateTime.UtcNow : null;
				}

				await repository.UpdateAsync(trip);
				_logger.LogInformation("Updated attendance for student {StudentId} at stop {StopId} in trip {TripId}", studentId, stopId, tripId);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating attendance: {TripId}, {StopId}, {StudentId}", tripId, stopId, studentId);
				throw;
			}
		}

		public async Task<bool> CascadeDeactivateTripsByRouteAsync(Guid routeId)
		{
			try
			{
				var repository = _databaseFactory.GetRepositoryByType<ITripRepository>(DatabaseType.MongoDb);
				var trips = await repository.GetTripsByRouteAsync(routeId);
				
				if (!trips.Any())
					return true;

				var updateCount = 0;
				foreach (var trip in trips)
				{
					// Only deactivate scheduled trips
					if (trip.Status == TripStatus.Scheduled)
					{
						trip.Status = TripStatus.Cancelled;
						trip.UpdatedAt = DateTime.UtcNow;
						await repository.UpdateAsync(trip);
						updateCount++;
					}
				}

				_logger.LogInformation("Cascade deactivated {Count} trips for route {RouteId}", updateCount, routeId);
				return updateCount > 0;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error cascade deactivating trips for route: {RouteId}", routeId);
				throw;
			}
		}

	}
}
