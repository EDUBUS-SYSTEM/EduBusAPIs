using Constants;
using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Services.Contracts;
using Services.Models.Notification;
using Services.Models.Trip;
using Utils;

namespace Services.Implementations
{
	public class TripService : ITripService
	{
        const double ARRIVAL_THRESHOLD_KM = 0.3; //300 meters

        private readonly IDatabaseFactory _databaseFactory;
		private readonly ILogger<TripService> _logger;
		private readonly IStudentRepository _studentRepository;
		private readonly IPickupPointRepository _pickupPointRepository;
		private readonly IStudentPickupPointHistoryRepository _studentPickupPointHistoryRepository;
		private readonly IVehicleRepository _vehicleRepository;
		private readonly IMongoDatabase _mongoDatabase; 
		private readonly IVietMapService _vietMapService;
		private readonly INotificationService _notificationService;
		private readonly ITripHubService? _tripHubService;

		public TripService(
			IDatabaseFactory databaseFactory, 
			ILogger<TripService> logger,
            IMongoDatabase mongoDatabase,
            IStudentRepository studentRepository,
			IPickupPointRepository pickupPointRepository,
			IStudentPickupPointHistoryRepository studentPickupPointHistoryRepository,
			IVehicleRepository vehicleRepository,
			IVietMapService vietMapService,
			INotificationService notificationService,
			ITripHubService? tripHubService = null)
		{
			_databaseFactory = databaseFactory;
			_logger = logger;
			_studentRepository = studentRepository;
			_pickupPointRepository = pickupPointRepository;
			_studentPickupPointHistoryRepository = studentPickupPointHistoryRepository;
			_vehicleRepository = vehicleRepository;
			_vietMapService = vietMapService;
			_notificationService = notificationService;
            _mongoDatabase = mongoDatabase; 
			_tripHubService = tripHubService;
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
				var trips = await repository.FindByFilterAsync(filter, sort, skip, perPage);
				var tripsList = trips.ToList();

				// Populate stops with pickup point names for all trips
				await PopulateStopsWithPickupPointNamesForTripsAsync(tripsList);

				// Decrypt vehicle plates
				foreach (var trip in tripsList)
				{
					if (trip.Vehicle != null && trip.VehicleId != Guid.Empty)
					{
						try
						{
							var vehicle = await _vehicleRepository.FindAsync(trip.VehicleId);
							if (vehicle != null && vehicle.HashedLicensePlate != null && vehicle.HashedLicensePlate.Length > 0)
							{
								trip.Vehicle.MaskedPlate = SecurityHelper.DecryptFromBytes(vehicle.HashedLicensePlate);
							}
						}
						catch (Exception ex)
						{
							_logger.LogWarning(ex, "Error decrypting vehicle plate for trip {TripId}. Using existing masked plate.", trip.Id);
						}
					}
				}

				return tripsList;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error querying trips with pagination/sorting");
				throw;
			}
		}

		public async Task<TripListResponse> QueryTripsWithPaginationAsync(
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

				// Build filters
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

				var tripsCollection = _mongoDatabase.GetCollection<Trip>("trips");
				var totalCount = await tripsCollection.CountDocumentsAsync(filter);

				// Build sort
				var desc = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
				SortDefinition<Trip> sort = sortBy?.ToLowerInvariant() switch
				{
					"plannedstartat" => desc ? Builders<Trip>.Sort.Descending(x => x.PlannedStartAt) : Builders<Trip>.Sort.Ascending(x => x.PlannedStartAt),
					"plannedendat" => desc ? Builders<Trip>.Sort.Descending(x => x.PlannedEndAt) : Builders<Trip>.Sort.Ascending(x => x.PlannedEndAt),
					"status" => desc ? Builders<Trip>.Sort.Descending(x => x.Status) : Builders<Trip>.Sort.Ascending(x => x.Status),
					"servicedate" or _ => desc ? Builders<Trip>.Sort.Descending(x => x.ServiceDate) : Builders<Trip>.Sort.Ascending(x => x.ServiceDate),
				};

				// Get paginated trips
				var skip = (page - 1) * perPage;
				var trips = await repository.FindByFilterAsync(filter, sort, skip, perPage);
				var tripsList = trips.ToList();

				// Populate stops with pickup point names for all trips
				await PopulateStopsWithPickupPointNamesForTripsAsync(tripsList);

				// Decrypt vehicle plates
				var vehicleRepo = _databaseFactory.GetRepository<IVehicleRepository>();
				foreach (var trip in tripsList)
				{
					if (trip.Vehicle != null && trip.VehicleId != Guid.Empty)
					{
						try
						{
							var vehicle = await vehicleRepo.FindAsync(trip.VehicleId);
							if (vehicle != null && vehicle.HashedLicensePlate != null && vehicle.HashedLicensePlate.Length > 0)
							{
								trip.Vehicle.MaskedPlate = SecurityHelper.DecryptFromBytes(vehicle.HashedLicensePlate);
							}
						}
						catch (Exception ex)
						{
							_logger.LogWarning(ex, "Error decrypting vehicle plate for trip {TripId}. Using existing masked plate.", trip.Id);
						}
					}
				}

				// Calculate total pages
				var totalPages = (int)Math.Ceiling(totalCount / (double)perPage);

				// Create and return response
				return new TripListResponse
				{
					Trips = tripsList,
					TotalCount = (int)totalCount,
					Page = page,
					PerPage = perPage,
					TotalPages = totalPages
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error querying trips with pagination");
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

		// Populate attendance for all stops with active students
		await PopulateAttendanceForTripStopsAsync(trip);

		trip.Status = TripStatus.Scheduled;

		// Populate snapshots if VehicleId or DriverVehicleId are provided
		await PopulateTripSnapshotsAsync(trip);

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

			// Populate snapshots if VehicleId or DriverVehicleId are provided or changed
			if (trip.VehicleId != existingTrip.VehicleId || trip.DriverVehicleId != existingTrip.DriverVehicleId)
			{
				await PopulateTripSnapshotsAsync(trip);
			}

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

		public async Task<IEnumerable<Trip>> GetUpcomingTripsAsync(DateTime fromDate, int days = 7)
		{
			try
			{
				var repository = _databaseFactory.GetRepositoryByType<ITripRepository>(DatabaseType.MongoDb);
				var trips = await repository.GetUpcomingTripsAsync(fromDate, days);
				var tripsList = trips.ToList();
				
				// Populate stops with pickup point names for all trips
				await PopulateStopsWithPickupPointNamesForTripsAsync(tripsList);
				
				// Decrypt vehicle plates
				foreach (var trip in tripsList)
				{
					if (trip.Vehicle != null && trip.VehicleId != Guid.Empty)
					{
						try
						{
							var vehicle = await _vehicleRepository.FindAsync(trip.VehicleId);
							if (vehicle != null && vehicle.HashedLicensePlate != null && vehicle.HashedLicensePlate.Length > 0)
							{
								trip.Vehicle.MaskedPlate = SecurityHelper.DecryptFromBytes(vehicle.HashedLicensePlate);
							}
						}
						catch (Exception ex)
						{
							_logger.LogWarning(ex, "Error decrypting vehicle plate for trip {TripId}. Using existing masked plate.", trip.Id);
						}
					}
				}
				
				return tripsList;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting upcoming trips from: {FromDate} for {Days} days", fromDate, days);
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
						try
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

							// Get Route to access VehicleId and PickupPoints
							var routeRepo = _databaseFactory.GetRepositoryByType<IMongoRepository<Route>>(DatabaseType.MongoDb);
							var route = await routeRepo.FindAsync(link.RouteId);
							if (route == null || route.IsDeleted || !route.IsActive)
							{
								continue;
							}

							_logger.LogDebug("Found route {RouteId} with {PickupPointCount} pickup points and vehicle {VehicleId}", 
								link.RouteId, route.PickupPoints?.Count ?? 0, route.VehicleId);

							// Find active DriverVehicle for this vehicle on serviceDate
							Guid? driverVehicleId = null;
							Trip.DriverSnapshot? driverSnapshot = null;
							if (route.VehicleId != Guid.Empty)
							{
								var driverVehicleRepo = _databaseFactory.GetRepository<IDriverVehicleRepository>();
								var startOfDay = date.Date;
								var endOfDay = startOfDay.AddDays(1);
								
								_logger.LogInformation("Looking for active DriverVehicle for vehicle {VehicleId} on serviceDate {ServiceDate}", 
									route.VehicleId, date);
								
								// First try: Find by serviceDate
								var activeDriverVehicle = await driverVehicleRepo.GetActiveDriverVehicleForVehicleByDateAsync(route.VehicleId, date);
								
								// Fallback: If not found by date, try to get current active assignment
								if (activeDriverVehicle == null)
								{
									_logger.LogWarning("No DriverVehicle found for vehicle {VehicleId} on {ServiceDate}. Trying to find current active assignment.", 
										route.VehicleId, date);
									
									var activeAssignments = await driverVehicleRepo.GetActiveAssignmentsByVehicleAsync(route.VehicleId);
									activeDriverVehicle = activeAssignments.FirstOrDefault();
									
									if (activeDriverVehicle != null)
									{
										_logger.LogInformation("Found current active DriverVehicle {DriverVehicleId} for vehicle {VehicleId}", 
											activeDriverVehicle.Id, route.VehicleId);
									}
								}
								
								if (activeDriverVehicle != null)
								{
									_logger.LogInformation("Found DriverVehicle {DriverVehicleId} with DriverId={DriverId}, IsPrimary={IsPrimary}, StartTime={StartTime}, EndTime={EndTime}", 
										activeDriverVehicle.Id, 
										activeDriverVehicle.DriverId, 
										activeDriverVehicle.IsPrimaryDriver,
										activeDriverVehicle.StartTimeUtc,
										activeDriverVehicle.EndTimeUtc);
									
									// Check if Driver is loaded and valid
									if (activeDriverVehicle.Driver == null)
									{
										_logger.LogWarning("DriverVehicle {DriverVehicleId} has null Driver. Attempting to reload with Driver included.", 
											activeDriverVehicle.Id);
										
										// Try to reload with Driver included
										var driverVehicleWithDriver = await driverVehicleRepo.FindByConditionAsync(
											dv => dv.Id == activeDriverVehicle.Id && !dv.IsDeleted,
											dv => dv.Driver
										);
										activeDriverVehicle = driverVehicleWithDriver.FirstOrDefault();
									}
									
									if (activeDriverVehicle != null && activeDriverVehicle.Driver != null && !activeDriverVehicle.Driver.IsDeleted)
									{
										driverVehicleId = activeDriverVehicle.Id;
										driverSnapshot = new Trip.DriverSnapshot
										{
											Id = activeDriverVehicle.Driver.Id,
											FullName = $"{activeDriverVehicle.Driver.FirstName} {activeDriverVehicle.Driver.LastName}".Trim(),
											Phone = activeDriverVehicle.Driver.PhoneNumber ?? string.Empty,
											IsPrimary = activeDriverVehicle.IsPrimaryDriver,
											SnapshottedAtUtc = DateTime.UtcNow
										};
										
										_logger.LogInformation("Successfully populated Driver snapshot: DriverId={DriverId}, FullName={FullName}, Phone={Phone}, IsPrimary={IsPrimary}", 
											driverSnapshot.Id, driverSnapshot.FullName, driverSnapshot.Phone, driverSnapshot.IsPrimary);
									}
									else
									{
										_logger.LogWarning("DriverVehicle {DriverVehicleId} found but Driver is null or deleted. DriverId={DriverId}, DriverNull={DriverNull}, IsDeleted={IsDeleted}", 
											activeDriverVehicle.Id, 
											activeDriverVehicle.DriverId,
											activeDriverVehicle.Driver == null,
											activeDriverVehicle.Driver?.IsDeleted ?? true);
									}
								}
								else
								{
									_logger.LogWarning("No active driver-vehicle assignment found for vehicle {VehicleId} on {ServiceDate}. Trip will be created without driver assignment.", 
										route.VehicleId, date);
								}
							}

							var trip = new Trip
							{
								RouteId = link.RouteId,
								ServiceDate = date,
								PlannedStartAt = utcStart,
								PlannedEndAt = utcEnd,
								Status = TripStatus.Scheduled,
								VehicleId = route.VehicleId,
								DriverVehicleId = driverVehicleId,
								Driver = driverSnapshot,
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

						// Generate stops from route pickup points (pass route to avoid re-querying)
						_logger.LogDebug("Generating stops for trip on route {RouteId}", link.RouteId);
						await GenerateTripStopsFromRouteAsync(trip, route);
						_logger.LogDebug("Generated {StopCount} stops for trip", trip.Stops.Count);

						// Populate attendance for all stops with active students
						await PopulateAttendanceForTripStopsAsync(trip);

						// Populate snapshots (driver, vehicle) if VehicleId or DriverVehicleId are set
							_logger.LogDebug("Populating snapshots for trip with VehicleId={VehicleId}, DriverVehicleId={DriverVehicleId}", 
								trip.VehicleId, trip.DriverVehicleId);
							await PopulateTripSnapshotsAsync(trip);

							trip = await tripRepo.AddAsync(trip);
							created.Add(trip);
							
							_logger.LogInformation("Successfully generated trip {TripId} for route {RouteId} on {ServiceDate} with {StopCount} stops, VehicleId={VehicleId}, DriverVehicleId={DriverVehicleId}", 
								trip.Id, link.RouteId, date, trip.Stops.Count, trip.VehicleId, trip.DriverVehicleId);
						}
						catch (Exception ex)
						{
							_logger.LogError(ex, "Error generating trip for route {RouteId} on {ServiceDate}. Exception: {ExceptionMessage}. StackTrace: {StackTrace}. Skipping this route.", 
								link.RouteId, date, ex.Message, ex.StackTrace);
							// Continue with next route instead of failing entire batch
							continue;
						}
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

			ValidateTripTimeConstraints(trip);

			var currentDate = DateTime.UtcNow.Date;
			if (trip.ServiceDate.Date < currentDate)
				throw new ArgumentException($"Service date ({trip.ServiceDate:yyyy-MM-dd}) cannot be in the past. Current date is {currentDate:yyyy-MM-dd}");

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

				ValidateTripTimesAgainstSchedule(trip, schedule);
			}

			ValidateTripPickupPoints(trip, route);

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

			ValidateTripTimeConstraints(trip);

			// Validate Route exists and is active
			var routeRepo = _databaseFactory.GetRepositoryByType<IMongoRepository<Route>>(DatabaseType.MongoDb);
			var route = await routeRepo.FindAsync(trip.RouteId);
			if (route == null || route.IsDeleted || !route.IsActive)
				throw new InvalidOperationException($"Route {trip.RouteId} does not exist or is inactive");

			if (trip.ScheduleSnapshot != null && trip.ScheduleSnapshot.ScheduleId != Guid.Empty)
			{
				var scheduleRepo = _databaseFactory.GetRepositoryByType<IMongoRepository<Schedule>>(DatabaseType.MongoDb);
				var schedule = await scheduleRepo.FindAsync(trip.ScheduleSnapshot.ScheduleId);
				if (schedule == null || schedule.IsDeleted || !schedule.IsActive)
					throw new InvalidOperationException($"Schedule {trip.ScheduleSnapshot.ScheduleId} does not exist or is inactive");

				ValidateTripTimesAgainstSchedule(trip, schedule);
			}

			ValidateTripPickupPoints(trip, route);
		}

		public static void ValidateTripTimeConstraints(Trip trip)
		{
			if (trip.PlannedStartAt >= trip.PlannedEndAt)
				throw new ArgumentException("Planned start time must be before planned end time");

			var duration = trip.PlannedEndAt - trip.PlannedStartAt;
			if (duration.TotalMinutes < 15)
				throw new ArgumentException("Trip duration must be at least 15 minutes");

			//if (duration.TotalHours > 8)
			//	throw new ArgumentException("Trip duration cannot exceed 8 hours");

			var now = DateTime.UtcNow;
			if (trip.ServiceDate.Date == now.Date)
			{
				if (trip.PlannedStartAt < now.AddMinutes(-5)) // Allow 5 minutes buffer
					throw new ArgumentException($"Planned start time ({trip.PlannedStartAt:HH:mm}) cannot be more than 5 minutes in the past for today's trips");
			}
		}

		public static void ValidateTripTimesAgainstSchedule(Trip trip, Schedule schedule)
		{
			if (!TryParseHms(schedule.StartTime, out var sh, out var sm, out var ss) ||
				!TryParseHms(schedule.EndTime, out var eh, out var em, out var es))
			{
				throw new ArgumentException($"Invalid schedule time format: StartTime={schedule.StartTime}, EndTime={schedule.EndTime}");
			}

			var scheduleStart = new DateTime(trip.ServiceDate.Year, trip.ServiceDate.Month, trip.ServiceDate.Day, sh, sm, ss, DateTimeKind.Unspecified);
			var scheduleEnd = new DateTime(trip.ServiceDate.Year, trip.ServiceDate.Month, trip.ServiceDate.Day, eh, em, es, DateTimeKind.Unspecified);
			
			if (scheduleEnd <= scheduleStart)
				scheduleEnd = scheduleEnd.AddDays(1);

			// Validate trip times are within reasonable range of schedule times (±30 minutes)
			var startDiff = Math.Abs((trip.PlannedStartAt - scheduleStart).TotalMinutes);
			var endDiff = Math.Abs((trip.PlannedEndAt - scheduleEnd).TotalMinutes);

			if (startDiff > 30)
				throw new ArgumentException($"Trip start time ({trip.PlannedStartAt:HH:mm}) deviates more than 30 minutes from schedule start time ({scheduleStart:HH:mm})");

			if (endDiff > 30)
				throw new ArgumentException($"Trip end time ({trip.PlannedEndAt:HH:mm}) deviates more than 30 minutes from schedule end time ({scheduleEnd:HH:mm})");
		}

	public static void ValidateTripPickupPoints(Trip trip, Route route)
	{
		if (trip.Stops == null || !trip.Stops.Any())
			return; 

		var routePickupPointIds = route.PickupPoints.Select(pp => pp.PickupPointId).ToHashSet();
		var invalidPickupPoints = new List<Guid>();

		foreach (var stop in trip.Stops)
		{
			// Skip stops with empty PickupPointId (they are filtered out elsewhere)
			if (stop.PickupPointId == Guid.Empty)
				continue;

			if (!routePickupPointIds.Contains(stop.PickupPointId))
			{
				invalidPickupPoints.Add(stop.PickupPointId);
			}
		}

		if (invalidPickupPoints.Any())
		{
			throw new InvalidOperationException($"The following pickup points do not belong to route {trip.RouteId}: {string.Join(", ", invalidPickupPoints)}");
		}
	}

		private async Task GenerateTripStopsFromRouteAsync(Trip trip, Route? route = null)
		{
			if (trip.Stops.Any())
				return; // Already has stops

			// If route is not provided, fetch it from database
			if (route == null)
			{
				var routeRepo = _databaseFactory.GetRepositoryByType<IMongoRepository<Route>>(DatabaseType.MongoDb);
				route = await routeRepo.FindAsync(trip.RouteId);
				if (route == null)
					throw new InvalidOperationException($"Route {trip.RouteId} not found");
			}

			if (route.PickupPoints == null || !route.PickupPoints.Any())
			{
				_logger.LogWarning("Route {RouteId} has no pickup points. Trip will be created without stops.", trip.RouteId);
				trip.Stops = new List<TripStop>();
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

			_logger.LogInformation("Generated {Count} trip stops for trip on route {RouteId}", trip.Stops.Count, trip.RouteId);
		}

		/// Populates attendance list for each trip stop with active students assigned to the pickup point
		private async Task PopulateAttendanceForTripStopsAsync(Trip trip)
		{
			if (trip.Stops == null || !trip.Stops.Any())
			{
				_logger.LogDebug("Trip {TripId} has no stops to populate attendance for", trip.Id);
				return;
			}

			try
			{
				// Get all unique pickup point IDs from stops
				var pickupPointIds = trip.Stops
					.Where(s => s.PickupPointId != Guid.Empty)
					.Select(s => s.PickupPointId)
					.Distinct()
					.ToList();

				if (!pickupPointIds.Any())
				{
					_logger.LogDebug("Trip {TripId} has no valid pickup point IDs in stops", trip.Id);
					return;
				}

				// Fetch all active students for all pickup points in one batch
				var allStudentsByPickupPoint = new Dictionary<Guid, List<Student>>();
				foreach (var pickupPointId in pickupPointIds)
				{
					var students = await _studentRepository.GetActiveStudentsByPickupPointIdAsync(pickupPointId);
					if (students.Any())
					{
						allStudentsByPickupPoint[pickupPointId] = students;
						_logger.LogDebug("Found {Count} active students for pickup point {PickupPointId}", 
							students.Count, pickupPointId);
					}
				}

				// Populate attendance for each stop
				foreach (var stop in trip.Stops)
				{
					if (stop.PickupPointId == Guid.Empty)
						continue;

					// Initialize attendance list if null
					if (stop.Attendance == null)
					{
						stop.Attendance = new List<Attendance>();
					}

					// Only populate if attendance is empty (to avoid overwriting existing attendance)
					if (!stop.Attendance.Any() && allStudentsByPickupPoint.TryGetValue(stop.PickupPointId, out var students))
					{
						stop.Attendance = students.Select(student => new Attendance
						{
							StudentId = student.Id,
							StudentName = $"{student.FirstName} {student.LastName}".Trim(),
							BoardedAt = null, // Not boarded yet
							State = Constants.AttendanceStates.Pending // Initial state is Pending
						}).ToList();

						_logger.LogDebug("Populated {Count} attendance records for stop {SequenceOrder} (pickup point {PickupPointId})", 
							stop.Attendance.Count, stop.SequenceOrder, stop.PickupPointId);
					}
				}

				var totalAttendance = trip.Stops.Sum(s => s.Attendance?.Count ?? 0);
				_logger.LogInformation("Populated attendance for trip {TripId}: {TotalAttendance} total attendance records across {StopCount} stops", 
					trip.Id, totalAttendance, trip.Stops.Count);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error populating attendance for trip {TripId}", trip.Id);
			}
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

		public async Task<bool> UpdateAttendanceAsync(Guid tripId, Guid? stopId, Guid studentId, string state)
		{
			try
			{
				ValidateAttendanceState(state);

				var repository = _databaseFactory.GetRepositoryByType<ITripRepository>(DatabaseType.MongoDb);
				var trip = await repository.FindAsync(tripId);
				if (trip == null)
					return false;

				Guid? actualStopId = stopId;

				// ✅ If stopId is not provided, derive it from student's CurrentPickupPointId
				if (!actualStopId.HasValue || actualStopId.Value == Guid.Empty)
				{
					var student = await _studentRepository.FindAsync(studentId);
					if (student == null || !student.CurrentPickupPointId.HasValue)
					{
						_logger.LogWarning("Cannot derive stopId for student {StudentId} - student not found or has no pickup point assigned", studentId);
						return false;
					}

					// Find the stop in this trip that matches the student's pickup point
					var matchingStop = trip.Stops.FirstOrDefault(s => s.PickupPointId == student.CurrentPickupPointId.Value);
					if (matchingStop == null)
					{
						_logger.LogWarning("Student {StudentId} has pickup point {PickupPointId} but no matching stop found in trip {TripId}",
							studentId, student.CurrentPickupPointId.Value, tripId);
						return false;
					}

					actualStopId = student.CurrentPickupPointId.Value;
					_logger.LogDebug("Derived stopId {StopId} from student {StudentId}'s CurrentPickupPointId", actualStopId.Value, studentId);
				}

				// Find the stop using the actualStopId (which is the PickupPointId)
				var stop = trip.Stops.FirstOrDefault(s => s.PickupPointId == actualStopId.Value);
				if (stop == null)
				{
					_logger.LogWarning("Stop with PickupPointId {StopId} not found in trip {TripId}", actualStopId.Value, tripId);
					return false;
				}

				// Find existing attendance or create new
				var attendance = stop.Attendance.FirstOrDefault(a => a.StudentId == studentId);
				if (attendance == null)
				{
					// Get student info to populate name
					var student = await _studentRepository.FindAsync(studentId);
					var studentName = student != null ? $"{student.FirstName} {student.LastName}".Trim() : string.Empty;

					attendance = new Attendance
					{
						StudentId = studentId,
						StudentName = studentName,
						State = state,
						BoardedAt = state == AttendanceStates.Present ? DateTime.UtcNow : null
					};
					stop.Attendance.Add(attendance);
				}
				else
				{
					// Update state and boarded time
					attendance.State = state;
					attendance.BoardedAt = state == AttendanceStates.Present ? DateTime.UtcNow : null;

					// Populate student name if missing (for backward compatibility)
					if (string.IsNullOrEmpty(attendance.StudentName))
					{
						var student = await _studentRepository.FindAsync(studentId);
						if (student != null)
						{
							attendance.StudentName = $"{student.FirstName} {student.LastName}".Trim();
						}
					}
				}

				// ✅ Update stop arrival/departure times based on attendance
				var now = DateTime.UtcNow;

				// Set ArrivedAt when first student is marked Present (vehicle arrived at stop)
				if (state == AttendanceStates.Present && !stop.ArrivedAt.HasValue)
				{
					stop.ArrivedAt = now;
					_logger.LogDebug("Set ArrivedAt for stop {StopId} in trip {TripId}", actualStopId.Value, tripId);
				}

				// Set DepartedAt when all students are accounted for (no pending students)
				var allAccountedFor = stop.Attendance.All(a =>
					a.State == AttendanceStates.Present ||
					a.State == AttendanceStates.Absent ||
					a.State == AttendanceStates.Excused ||
					a.State == AttendanceStates.Late);

				if (allAccountedFor && stop.ArrivedAt.HasValue && !stop.DepartedAt.HasValue)
				{
					stop.DepartedAt = now;
					_logger.LogDebug("Set DepartedAt for stop {StopId} in trip {TripId} - all students accounted for", actualStopId.Value, tripId);
				}

				await repository.UpdateAsync(trip);
				_logger.LogInformation("Updated attendance for student {StudentId} at stop {StopId} in trip {TripId}", studentId, actualStopId.Value, tripId);

				if (_tripHubService != null)
				{
					try
					{
						// Calculate attendance summary for the stop (including stop progress times)
						var attendanceSummary = new
						{
							total = stop.Attendance.Count,
							present = stop.Attendance.Count(a => a.State == Constants.AttendanceStates.Present),
							absent = stop.Attendance.Count(a => a.State == Constants.AttendanceStates.Absent),
							pending = stop.Attendance.Count(a => a.State == Constants.AttendanceStates.Pending),
							late = stop.Attendance.Count(a => a.State == Constants.AttendanceStates.Late),
							excused = stop.Attendance.Count(a => a.State == Constants.AttendanceStates.Excused),
							// ✅ Include stop progress times in the same broadcast
							arrivedAt = stop.ArrivedAt,
							departedAt = stop.DepartedAt
						};

						await _tripHubService.BroadcastAttendanceUpdatedAsync(
							tripId,
							actualStopId.Value,  // Use the actual stopId (PickupPointId)
							attendanceSummary);
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "Failed to broadcast attendance update for trip {TripId}, stop {StopId}", tripId, actualStopId.Value);
						// Don't fail the operation if broadcast fails
					}
				}

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating attendance: {TripId}, {StopId}, {StudentId}", tripId, stopId, studentId);
				throw;
			}
		}

		public static void ValidateAttendanceState(string state)
		{
			if (string.IsNullOrWhiteSpace(state))
				throw new ArgumentException("Attendance state cannot be null or empty");

			var validStates = new[]
			{
				AttendanceStates.Present,
				AttendanceStates.Absent,
				AttendanceStates.Late,
				AttendanceStates.Excused,
				AttendanceStates.Pending
			};

			if (!validStates.Contains(state))
			{
				throw new ArgumentException($"Invalid attendance state '{state}'. Valid states are: {string.Join(", ", validStates)}");
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

		public async Task<IEnumerable<Trip>> GetDriverScheduleByDateAsync(Guid driverId, DateTime serviceDate)
		{
			try
			{
				var tripRepo = _databaseFactory.GetRepositoryByType<ITripRepository>(DatabaseType.MongoDb);
				var driverVehicleRepo = _databaseFactory.GetRepository<IDriverVehicleRepository>();
				var routeRepo = _databaseFactory.GetRepositoryByType<IMongoRepository<Route>>(DatabaseType.MongoDb);
				
				// Get active driver-vehicle assignments for the service date
				var driverVehicles = await driverVehicleRepo.GetActiveDriverVehiclesByDateAsync(driverId, serviceDate);
				if (!driverVehicles.Any())
					return Enumerable.Empty<Trip>();

				var vehicleIds = driverVehicles.Select(dv => dv.VehicleId).ToList();
				
				// Get all routes for these vehicles
				var routes = new List<Route>();
				foreach (var vehicleId in vehicleIds)
				{
					var vehicleRoutes = await routeRepo.FindByConditionAsync(r => r.VehicleId == vehicleId && r.IsActive && !r.IsDeleted);
					routes.AddRange(vehicleRoutes);
				}

				if (!routes.Any())
					return Enumerable.Empty<Trip>();

				var routeIds = routes.Select(r => r.Id).ToList();
				
				// Get trips for these routes on the service date
				var trips = new List<Trip>();
				foreach (var routeId in routeIds)
				{
					var routeTrips = await tripRepo.GetTripsByRouteAsync(routeId);
					var dayTrips = routeTrips.Where(t => t.ServiceDate.Date == serviceDate.Date);
					trips.AddRange(dayTrips);
				}

				// Sort by planned start time
				return trips.OrderBy(t => t.PlannedStartAt);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting driver schedule by date: {DriverId}, {ServiceDate}", driverId, serviceDate);
				throw;
			}
		}

		public async Task<IEnumerable<Trip>> GetDriverScheduleByRangeAsync(Guid driverId, DateTime startDate, DateTime endDate)
		{
			try
			{
				var tripRepo = _databaseFactory.GetRepositoryByType<ITripRepository>(DatabaseType.MongoDb);
				var driverVehicleRepo = _databaseFactory.GetRepository<IDriverVehicleRepository>();
				var routeRepo = _databaseFactory.GetRepositoryByType<IMongoRepository<Route>>(DatabaseType.MongoDb);
				
				// Get active driver-vehicle assignments for the date range
				var driverVehicles = await driverVehicleRepo.GetActiveDriverVehiclesByDateRangeAsync(driverId, startDate, endDate);
				if (!driverVehicles.Any())
					return Enumerable.Empty<Trip>();

				var vehicleIds = driverVehicles.Select(dv => dv.VehicleId).ToList();
				
				// Get all routes for these vehicles
				var routes = new List<Route>();
				foreach (var vehicleId in vehicleIds)
				{
					var vehicleRoutes = await routeRepo.FindByConditionAsync(r => r.VehicleId == vehicleId && r.IsActive && !r.IsDeleted);
					routes.AddRange(vehicleRoutes);
				}

				if (!routes.Any())
					return Enumerable.Empty<Trip>();

				var routeIds = routes.Select(r => r.Id).ToList();
				
				// Get trips for these routes in the date range
				var trips = new List<Trip>();
				foreach (var routeId in routeIds)
				{
					var routeTrips = await tripRepo.GetTripsByRouteAsync(routeId);
					var rangeTrips = routeTrips.Where(t => t.ServiceDate.Date >= startDate.Date && t.ServiceDate.Date <= endDate.Date);
					trips.AddRange(rangeTrips);
				}

				// Sort by service date and planned start time
				return trips.OrderBy(t => t.ServiceDate).ThenBy(t => t.PlannedStartAt);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting driver schedule by range: {DriverId}, {StartDate} to {EndDate}", driverId, startDate, endDate);
				throw;
			}
		}

		public async Task<IEnumerable<Trip>> GetDriverUpcomingScheduleAsync(Guid driverId, int days = 7)
		{
			try
			{
				var startDate = DateTime.UtcNow.Date;
				var endDate = startDate.AddDays(days);
				
				return await GetDriverScheduleByRangeAsync(driverId, startDate, endDate);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting driver upcoming schedule: {DriverId}, {Days} days", driverId, days);
				throw;
			}
		}

		public async Task<DriverScheduleSummary> GetDriverScheduleSummaryAsync(Guid driverId, DateTime startDate, DateTime endDate)
		{
			try
			{
				var trips = await GetDriverScheduleByRangeAsync(driverId, startDate, endDate);
				var tripList = trips.ToList();

				var summary = new DriverScheduleSummary
				{
					DriverId = driverId,
					StartDate = startDate,
					EndDate = endDate,
					TotalTrips = tripList.Count,
					ScheduledTrips = tripList.Count(t => t.Status == TripStatus.Scheduled),
					InProgressTrips = tripList.Count(t => t.Status == TripStatus.InProgress),
					CompletedTrips = tripList.Count(t => t.Status == TripStatus.Completed),
					CancelledTrips = tripList.Count(t => t.Status == TripStatus.Cancelled),
					TotalWorkingHours = CalculateTotalWorkingHours(tripList),
					TripsByDate = tripList.GroupBy(t => t.ServiceDate.Date)
						.ToDictionary(g => g.Key, g => g.Count())
				};

				return summary;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting driver schedule summary: {DriverId}", driverId);
				throw;
			}
		}

		private static double CalculateTotalWorkingHours(IEnumerable<Trip> trips)
		{
			var totalMinutes = trips
				.Where(t => t.Status == TripStatus.Completed && t.StartTime.HasValue && t.EndTime.HasValue)
				.Sum(t => (t.EndTime!.Value - t.StartTime!.Value).TotalMinutes);
			
			return Math.Round(totalMinutes / 60.0, 2);
		}

		private async Task PopulateTripSnapshotsAsync(Trip trip)
		{
			try
			{
				// Validate routeId is provided
				if (trip.RouteId == Guid.Empty)
				{
					_logger.LogWarning("Trip {TripId} has empty RouteId. Cannot populate snapshots.", trip.Id);
					return;
				}

				// Get Route to access VehicleId and other route information
				var routeRepo = _databaseFactory.GetRepositoryByType<IMongoRepository<Route>>(DatabaseType.MongoDb);
				var route = await routeRepo.FindAsync(trip.RouteId);
				if (route == null || route.IsDeleted || !route.IsActive)
				{
					_logger.LogWarning("Route {RouteId} not found, deleted, or inactive for trip {TripId}. Cannot populate snapshots.", trip.RouteId, trip.Id);
					return;
				}

				// Step 1: Populate Vehicle Snapshot
				if (route.VehicleId != Guid.Empty)
				{
					try
					{
						var vehicleRepo = _databaseFactory.GetRepository<IVehicleRepository>();
						var vehicle = await vehicleRepo.FindAsync(route.VehicleId);
						if (vehicle != null)
						{
							string maskedPlate = string.Empty;
							if (vehicle.HashedLicensePlate != null && vehicle.HashedLicensePlate.Length > 0)
							{
								try
								{
									maskedPlate = SecurityHelper.DecryptFromBytes(vehicle.HashedLicensePlate);
								}
								catch (Exception convertEx)
								{
									_logger.LogWarning(convertEx, "Failed to decrypt HashedLicensePlate for vehicle {VehicleId}. Using empty string.", vehicle.Id);
									maskedPlate = string.Empty;
								}
							}
							else
							{
								_logger.LogWarning("Vehicle {VehicleId} has null or empty HashedLicensePlate.", vehicle.Id);
							}

							trip.Vehicle = new Trip.VehicleSnapshot
							{
								Id = vehicle.Id,
								MaskedPlate = maskedPlate,
								Capacity = vehicle.Capacity,
								Status = vehicle.Status.ToString()
							};

							// Update trip.VehicleId to match the route's vehicle
							trip.VehicleId = route.VehicleId;

							_logger.LogDebug("Populated vehicle snapshot for trip {TripId} from route {RouteId}, vehicle {VehicleId}", trip.Id, trip.RouteId, vehicle.Id);
						}
						else
						{
							_logger.LogWarning("Vehicle {VehicleId} not found for route {RouteId} on trip {TripId}. Vehicle snapshot will not be populated.", route.VehicleId, trip.RouteId, trip.Id);
						}
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "Error populating vehicle snapshot for trip {TripId} from route {RouteId}. Continuing without vehicle snapshot.", trip.Id, trip.RouteId);
					}
				}

				// Step 2: Get DriverVehicle assignments for the vehicle to find the driver
				if (route.VehicleId != Guid.Empty)
				{
					try
					{
						var driverVehicleRepo = _databaseFactory.GetRepository<IDriverVehicleRepository>();

						// Get active driver-vehicle assignments for this vehicle
						// We want assignments that are active at the trip's service date
						var serviceDate = trip.ServiceDate;
						var activeAssignments = await driverVehicleRepo.GetActiveAssignmentsByVehicleAsync(route.VehicleId);

						// Filter to assignments that are active on the service date
						var assignmentsOnServiceDate = activeAssignments
							.Where(dv => dv.StartTimeUtc.Date <= serviceDate.Date &&
										 (!dv.EndTimeUtc.HasValue || dv.EndTimeUtc.Value.Date >= serviceDate.Date) &&
										 dv.Status == DriverVehicleStatus.Assigned &&
										 !dv.IsDeleted &&
										 dv.Driver != null &&
										 !dv.Driver.IsDeleted)
							.ToList();

						if (assignmentsOnServiceDate.Any())
						{
							// Prefer primary driver, otherwise use the first active assignment
							var driverVehicle = assignmentsOnServiceDate
								.FirstOrDefault(dv => dv.IsPrimaryDriver)
								?? assignmentsOnServiceDate.FirstOrDefault();

							if (driverVehicle != null && driverVehicle.Driver != null)
							{
								trip.Driver = new Trip.DriverSnapshot
								{
									Id = driverVehicle.Driver.Id,
									FullName = $"{driverVehicle.Driver.FirstName} {driverVehicle.Driver.LastName}".Trim(),
									Phone = driverVehicle.Driver.PhoneNumber ?? string.Empty,
									IsPrimary = driverVehicle.IsPrimaryDriver,
									SnapshottedAtUtc = DateTime.UtcNow
								};

								// Update trip.DriverVehicleId to match the found assignment
								trip.DriverVehicleId = driverVehicle.Id;

								_logger.LogDebug("Populated driver snapshot for trip {TripId} from route {RouteId}, driver {DriverId} (DriverVehicleId: {DriverVehicleId})",
									trip.Id, trip.RouteId, driverVehicle.Driver.Id, driverVehicle.Id);
							}
							else
							{
								_logger.LogWarning("No valid driver found in active assignments for vehicle {VehicleId} on route {RouteId} for trip {TripId} on service date {ServiceDate}. Driver snapshot will not be populated.",
									route.VehicleId, trip.RouteId, trip.Id, serviceDate);
							}
						}
						else
						{
							_logger.LogWarning("No active driver-vehicle assignments found for vehicle {VehicleId} on route {RouteId} for trip {TripId} on service date {ServiceDate}. Driver snapshot will not be populated.",
								route.VehicleId, trip.RouteId, trip.Id, serviceDate);
						}
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "Error populating driver snapshot for trip {TripId} from route {RouteId}. Continuing without driver snapshot.", trip.Id, trip.RouteId);
					}
				}

				// Step 3: Get SupervisorVehicle assignments for the vehicle to find the supervisor
				if (route.VehicleId != Guid.Empty)
				{
					try
					{
						var supervisorVehicleRepo = _databaseFactory.GetRepository<ISupervisorVehicleRepository>();

						var serviceDate = trip.ServiceDate;
						var activeSupervisorAssignments = await supervisorVehicleRepo.GetActiveAssignmentsByVehicleAsync(route.VehicleId);

						var supervisorsOnServiceDate = activeSupervisorAssignments
							.Where(sv => sv.StartTimeUtc.Date <= serviceDate.Date &&
										 (!sv.EndTimeUtc.HasValue || sv.EndTimeUtc.Value.Date >= serviceDate.Date) &&
										 sv.Status == SupervisorVehicleStatus.Assigned &&
										 !sv.IsDeleted &&
										 sv.Supervisor != null &&
										 !sv.Supervisor.IsDeleted)
							.ToList();

						if (supervisorsOnServiceDate.Any())
						{
							var supervisorVehicle = supervisorsOnServiceDate.First();

							trip.Supervisor = new Trip.SupervisorSnapshot
							{
								Id = supervisorVehicle.Supervisor.Id,
								FullName = $"{supervisorVehicle.Supervisor.FirstName} {supervisorVehicle.Supervisor.LastName}".Trim(),
								Phone = supervisorVehicle.Supervisor.PhoneNumber ?? string.Empty,
								SnapshottedAtUtc = DateTime.UtcNow
							};

							trip.SupervisorVehicleId = supervisorVehicle.Id;

							_logger.LogDebug("Populated supervisor snapshot for trip {TripId} from route {RouteId}, supervisor {SupervisorId} (SupervisorVehicleId: {SupervisorVehicleId})",
								trip.Id, trip.RouteId, supervisorVehicle.Supervisor.Id, supervisorVehicle.Id);
						}
						else
						{
							_logger.LogWarning("No active supervisor assignments found for vehicle {VehicleId} on route {RouteId} for trip {TripId} on service date {ServiceDate}. Supervisor snapshot will not be populated.",
								route.VehicleId, trip.RouteId, trip.Id, serviceDate);
						}
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "Error populating supervisor snapshot for trip {TripId} from route {RouteId}. Continuing without supervisor snapshot.", trip.Id, trip.RouteId);
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error populating snapshots for trip {TripId} from route {RouteId}.", trip.Id, trip.RouteId);
				// Don't throw - allow trip to continue without snapshots
			}
		}

		public async Task<IEnumerable<Trip>> GetTripsByDateForDriverAsync(Guid driverId, DateTime? date = null)
		{
			try
		{
			var targetDate = (date ?? DateTime.UtcNow).Date;

			var tripRepo = _databaseFactory.GetRepositoryByType<ITripRepository>(DatabaseType.MongoDb);
			
			// Get trips by date first (without driver filter since driver snapshot may not be populated)
			var allTrips = await tripRepo.GetTripsByDateAsync(targetDate);

			// Populate snapshots for all trips
			var tripsList = allTrips.ToList();
			foreach (var trip in tripsList)
			{
				await PopulateTripSnapshotsAsync(trip);
			}

			// Filter by driver ID after populating snapshots
			var driverTrips = tripsList.Where(t => t.Driver?.Id == driverId).ToList();

			// Decrypt vehicle plates
			foreach (var trip in driverTrips)
			{
				if (trip.Vehicle != null && trip.VehicleId != Guid.Empty)
				{
					try
					{
						var vehicle = await _vehicleRepository.FindAsync(trip.VehicleId);
						if (vehicle != null && vehicle.HashedLicensePlate != null && vehicle.HashedLicensePlate.Length > 0)
						{
							trip.Vehicle.MaskedPlate = SecurityHelper.DecryptFromBytes(vehicle.HashedLicensePlate);
							_logger.LogDebug("Decrypted vehicle plate for trip {TripId}", trip.Id);
						}
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "Error decrypting vehicle plate for trip {TripId}. Using existing masked plate.", trip.Id);
					}
				}
			}

			return driverTrips;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error getting trips for driver: {DriverId}, Date: {Date}", driverId, date);
			throw;
		}
	}

		public async Task<Trip?> GetTripDetailForDriverAsync(Guid tripId, Guid driverId)
		{
			try
			{
				var tripRepo = _databaseFactory.GetRepositoryByType<ITripRepository>(DatabaseType.MongoDb);
				var trip = await tripRepo.FindAsync(tripId);
				
				if (trip == null || trip.IsDeleted)
					return null;

				// Populate vehicle and driver snapshots
				await PopulateTripSnapshotsAsync(trip);

				// Verify driver owns this trip
				if (trip.Driver?.Id != driverId)
					return null;

				await PopulateTripDetailAsync(trip);
				return trip;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting trip detail for driver: {TripId}, {DriverId}", tripId, driverId);
				throw;
			}
		}

		public async Task<Trip?> GetTripDetailForAdminAsync(Guid tripId)
		{
			try
			{
				var tripRepo = _databaseFactory.GetRepositoryByType<ITripRepository>(DatabaseType.MongoDb);
				var trip = await tripRepo.FindAsync(tripId);
				
				if (trip == null || trip.IsDeleted)
					return null;

				await PopulateTripSnapshotsAsync(trip);
				await PopulateTripDetailAsync(trip);
				return trip;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting trip detail for admin: {TripId}", tripId);
				throw;
			}
		}

		private async Task PopulateTripDetailAsync(Trip trip)
		{
			// Decrypt vehicle plate
			if (trip.Vehicle != null)
			{
				try
				{
					var vehicle = await _vehicleRepository.FindAsync(trip.VehicleId);
					if (vehicle != null && vehicle.HashedLicensePlate != null && vehicle.HashedLicensePlate.Length > 0)
					{
						trip.Vehicle.MaskedPlate = SecurityHelper.DecryptFromBytes(vehicle.HashedLicensePlate);
					}
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Error decrypting vehicle plate for trip {TripId}. Using existing masked plate.", trip.Id);
				}
			}

			// Populate stops with pickup point details
			if (trip.Stops != null && trip.Stops.Any())
			{
				try
				{
					var pickupPointIds = trip.Stops.Select(s => s.PickupPointId).Distinct().ToList();
					
					var pickupPoints = new Dictionary<Guid, PickupPoint>();
					foreach (var pickupPointId in pickupPointIds)
					{
						var pickupPoint = await _pickupPointRepository.FindAsync(pickupPointId);
						if (pickupPoint != null && !pickupPoint.IsDeleted)
						{
							pickupPoints[pickupPointId] = pickupPoint;
						}
					}

					// Update stops with pickup point information
					foreach (var stop in trip.Stops)
					{
						if (pickupPoints.TryGetValue(stop.PickupPointId, out var pickupPoint))
						{
							// Update location if not already set or if pickup point has more complete info
							if (stop.Location == null || string.IsNullOrEmpty(stop.Location.Address))
							{
								stop.Location = new LocationInfo
								{
									Latitude = pickupPoint.Geog?.Y ?? stop.Location?.Latitude ?? 0,
									Longitude = pickupPoint.Geog?.X ?? stop.Location?.Longitude ?? 0,
									Address = pickupPoint.Location ?? stop.Location?.Address ?? string.Empty
								};
							}
						}
					}
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Error populating stops with pickup point details for trip {TripId}. Continuing with existing stop data.", trip.Id);
				}
			}
		}

		public async Task<bool> StartTripAsync(Guid tripId, Guid driverId)
		{
			try
			{
				var tripRepo = _databaseFactory.GetRepositoryByType<ITripRepository>(DatabaseType.MongoDb);
				var trip = await tripRepo.FindAsync(tripId);
				
				if (trip == null || trip.IsDeleted)
					return false;

				// Verify driver owns this trip
				if (trip.Driver?.Id != driverId)
					return false;

				// Check if trip can be started
				if (trip.Status != Constants.TripStatus.Scheduled)
				{
					_logger.LogWarning("Cannot start trip {TripId} with status {Status}", tripId, trip.Status);
					return false;
				}

				// Update trip status and start time
				trip.Status = Constants.TripStatus.InProgress;
				trip.StartTime = DateTime.UtcNow;

				await tripRepo.UpdateAsync(trip);
				_logger.LogInformation("Trip {TripId} started by driver {DriverId}", tripId, driverId);

				if (_tripHubService != null)
				{
					try
					{
						await _tripHubService.BroadcastTripStatusChangedAsync(
							tripId,
							trip.Status,
							trip.StartTime,
							null);
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "Failed to broadcast trip status change for trip {TripId}", tripId);
						// Don't fail the operation if broadcast fails
					}
				}

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error starting trip: {TripId}, {DriverId}", tripId, driverId);
				throw;
			}
		}

		public async Task<bool> EndTripAsync(Guid tripId, Guid driverId)
		{
			try
			{
				var tripRepo = _databaseFactory.GetRepositoryByType<ITripRepository>(DatabaseType.MongoDb);
				var trip = await tripRepo.FindAsync(tripId);
				
				if (trip == null || trip.IsDeleted)
					return false;

				// Verify driver owns this trip
				if (trip.Driver?.Id != driverId)
					return false;

				// Check if trip can be ended
				if (trip.Status != Constants.TripStatus.InProgress)
				{
					_logger.LogWarning("Cannot end trip {TripId} with status {Status}", tripId, trip.Status);
					return false;
				}

				// Update trip status and end time
				trip.Status = Constants.TripStatus.Completed;
				trip.EndTime = DateTime.UtcNow;

				await tripRepo.UpdateAsync(trip);
				_logger.LogInformation("Trip {TripId} ended by driver {DriverId}", tripId, driverId);

				if (_tripHubService != null)
				{
					try
					{
						await _tripHubService.BroadcastTripStatusChangedAsync(
							tripId,
							trip.Status,
							trip.StartTime,
							trip.EndTime);
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "Failed to broadcast trip status change for trip {TripId}", tripId);
						// Don't fail the operation if broadcast fails
					}
				}

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error ending trip: {TripId}, {DriverId}", tripId, driverId);
				throw;
			}
		}

		public async Task<bool> UpdateTripLocationAsync(Guid tripId, Guid driverId, double latitude, double longitude, double? speed = null, double? accuracy = null, bool isMoving = false)
		{
			try
			{
				var tripRepo = _databaseFactory.GetRepositoryByType<ITripRepository>(DatabaseType.MongoDb);
				var trip = await tripRepo.FindAsync(tripId);
				
				if (trip == null || trip.IsDeleted)
					return false;

				// Verify driver owns this trip
				if (trip.Driver?.Id != driverId)
					return false;

                var location = new Trip.VehicleLocation
                {
                    Latitude = latitude,
                    Longitude = longitude,
                    RecordedAt = DateTime.UtcNow,
                    Speed = speed,
                    Accuracy = accuracy,
                    IsMoving = isMoving
                };

                // Update current location
                trip.CurrentLocation = location;
                await tripRepo.UpdateAsync(trip);

                // Save location in TripLocationHistory using repository
                var historyRepo = _databaseFactory.GetRepositoryByType<ITripLocationHistoryRepository>(DatabaseType.MongoDb);
                var historyRecord = new TripLocationHistory
                {
                    TripId = tripId,
                    Location = location
                };
                await historyRepo.AddAsync(historyRecord);
                return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating trip location: {TripId}, {DriverId}", tripId, driverId);
				throw;
			}
		}

		public async Task<Trip?> GetTripWithStopsAsync(Guid tripId)
		{
			try
			{
				var trip = await GetTripDetailForAdminAsync(tripId);
				if (trip == null)
					return null;

				// Populate stops with pickup point names
				await PopulateStopsWithPickupPointNamesAsync(trip);
				return trip;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting trip with stops: {TripId}", tripId);
				throw;
			}
		}

		public async Task<IEnumerable<Trip>> GetTripsByDateWithDetailsAsync(DateTime serviceDate)
		{
			try
			{
				var trips = await GetTripsByDateAsync(serviceDate);
				var tripsList = trips.ToList();
				
				// Decrypt vehicle plates
				foreach (var trip in tripsList)
				{
					if (trip.Vehicle != null && trip.VehicleId != Guid.Empty)
					{
						try
						{
							var vehicle = await _vehicleRepository.FindAsync(trip.VehicleId);
							if (vehicle != null && vehicle.HashedLicensePlate != null && vehicle.HashedLicensePlate.Length > 0)
							{
								trip.Vehicle.MaskedPlate = SecurityHelper.DecryptFromBytes(vehicle.HashedLicensePlate);
							}
						}
						catch (Exception ex)
						{
							_logger.LogWarning(ex, "Error decrypting vehicle plate for trip {TripId}. Using existing masked plate.", trip.Id);
						}
					}
				}

				// Populate stops with pickup point names for all trips
				await PopulateStopsWithPickupPointNamesForTripsAsync(tripsList);

				return tripsList;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting trips by date with details: {ServiceDate}", serviceDate);
				throw;
			}
		}

		public async Task<Trip?> GetTripDetailForDriverWithStopsAsync(Guid tripId, Guid driverId)
		{
			try
			{
				var trip = await GetTripDetailForDriverAsync(tripId, driverId);
				if (trip == null)
					return null;

				// Populate stops with pickup point names
				await PopulateStopsWithPickupPointNamesAsync(trip);
				return trip;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting trip detail for driver with stops: {TripId}, {DriverId}", tripId, driverId);
				throw;
			}
		}

		public async Task<object> GenerateAllTripsAutomaticAsync(int daysAhead = 7)
		{
			try
			{
				var startDate = DateTime.UtcNow.Date;
				var endDate = startDate.AddDays(daysAhead);

				var scheduleRepo = _databaseFactory.GetRepositoryByType<IScheduleRepository>(DatabaseType.MongoDb);
				var routeScheduleRepo = _databaseFactory.GetRepositoryByType<IRouteScheduleRepository>(DatabaseType.MongoDb);

				// Get all active schedules
				var activeSchedules = await scheduleRepo.FindByFilterAsync(
					Builders<Schedule>.Filter.And(
						Builders<Schedule>.Filter.Eq(s => s.IsActive, true),
						Builders<Schedule>.Filter.Eq(s => s.IsDeleted, false),
						Builders<Schedule>.Filter.Lte(s => s.EffectiveFrom, endDate),
						Builders<Schedule>.Filter.Or(
							Builders<Schedule>.Filter.Eq(s => s.EffectiveTo, null),
							Builders<Schedule>.Filter.Gte(s => s.EffectiveTo, startDate)
						)
					)
				);

				var totalGenerated = 0;
				var processedSchedules = 0;
				var results = new List<object>();

				foreach (var schedule in activeSchedules)
				{
					try
					{
						// Check if schedule has active route schedules
						var routeSchedules = await routeScheduleRepo.GetRouteSchedulesByScheduleAsync(schedule.Id);
						var activeRouteSchedules = routeSchedules.Where(rs => rs.IsActive && !rs.IsDeleted).ToList();

						if (!activeRouteSchedules.Any())
							continue;

						// Generate trips for this schedule
						var generatedTrips = await GenerateTripsFromScheduleAsync(
							schedule.Id, 
							startDate, 
							endDate
						);

						var tripCount = generatedTrips.Count();
						totalGenerated += tripCount;
						processedSchedules++;

						results.Add(new
						{
							scheduleId = schedule.Id,
							scheduleName = schedule.Name,
							tripCount = tripCount,
							routeScheduleCount = activeRouteSchedules.Count
						});
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Error generating trips for schedule {ScheduleId}", schedule.Id);
						results.Add(new
						{
							scheduleId = schedule.Id,
							scheduleName = schedule.Name,
							error = ex.Message
						});
					}
				}

				return new
				{
					message = "Automatic trip generation completed",
					startDate = startDate,
					endDate = endDate,
					processedSchedules = processedSchedules,
					totalGenerated = totalGenerated,
					results = results
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in automatic trip generation");
				throw;
			}
		}

		private async Task PopulateStopsWithPickupPointNamesAsync(Trip trip)
		{
			if (trip.Stops == null || !trip.Stops.Any())
				return;

			try
			{
				var pickupPointIds = trip.Stops
					.Select(s => s.PickupPointId)
					.Where(id => id != Guid.Empty)
					.Distinct()
					.ToList();
				
				var pickupPoints = new Dictionary<Guid, PickupPoint>();
				foreach (var pickupPointId in pickupPointIds)
				{
					var pickupPoint = await _pickupPointRepository.FindAsync(pickupPointId);
					if (pickupPoint != null && !pickupPoint.IsDeleted)
					{
						pickupPoints[pickupPointId] = pickupPoint;
					}
				}

				// Update stops with pickup point information
				foreach (var stop in trip.Stops)
				{
					if (pickupPoints.TryGetValue(stop.PickupPointId, out var pickupPoint))
					{
						// Update location with pickup point info
						stop.Location = new LocationInfo
						{
							Latitude = pickupPoint.Geog?.Y ?? stop.Location?.Latitude ?? 0,
							Longitude = pickupPoint.Geog?.X ?? stop.Location?.Longitude ?? 0,
							Address = pickupPoint.Location ?? stop.Location?.Address ?? string.Empty
						};
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Error populating stops with pickup point names for trip {TripId}. Continuing with existing stop data.", trip.Id);
			}
		}

		private async Task PopulateStopsWithPickupPointNamesForTripsAsync(List<Trip> trips)
		{
			if (trips == null || !trips.Any())
				return;

			try
			{
				
				// Collect all unique pickup point IDs from all trips (skip empty GUIDs)
				var allPickupPointIds = trips
					.Where(t => t.Stops != null && t.Stops.Any())
					.SelectMany(t => t.Stops.Select(s => s.PickupPointId))
					.Where(id => id != Guid.Empty)
					.Distinct()
					.ToList();

				// Load all pickup points at once
				var pickupPoints = new Dictionary<Guid, PickupPoint>();
				foreach (var pickupPointId in allPickupPointIds)
				{
					var pickupPoint = await _pickupPointRepository.FindAsync(pickupPointId);
					if (pickupPoint != null && !pickupPoint.IsDeleted)
					{
						pickupPoints[pickupPointId] = pickupPoint;
					}
				}

				// Update stops for each trip
				foreach (var trip in trips)
				{
					if (trip.Stops != null && trip.Stops.Any())
					{
						foreach (var stop in trip.Stops)
						{
							// Skip stops with empty PickupPointId
							if (stop.PickupPointId == Guid.Empty)
								continue;

							if (pickupPoints.TryGetValue(stop.PickupPointId, out var pickupPoint))
							{
								// Update location with pickup point info
								stop.Location = new LocationInfo
								{
									Latitude = pickupPoint.Geog?.Y ?? stop.Location?.Latitude ?? 0,
									Longitude = pickupPoint.Geog?.X ?? stop.Location?.Longitude ?? 0,
									Address = pickupPoint.Description ?? pickupPoint.Location ?? stop.Location?.Address ?? string.Empty
								};
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Error populating stops with pickup point names for trips. Continuing with existing stop data.");
			}
		}

		public async Task<IEnumerable<Trip>> GetTripsByScheduleForParentAsync(string parentEmail, int days = 7)
		{
			try
			{
				var fromDate = DateTime.UtcNow.Date;
				var toDate = fromDate.AddDays(days);

				var students = await _studentRepository.GetStudentsByParentEmailAsync(parentEmail);
				
				if (!students.Any())
					return Enumerable.Empty<Trip>();

				var studentIds = students.Select(s => s.Id).ToList();
				var pickupPointIds = await GetPickupPointIdsForStudentsAsync(studentIds);

				if (!pickupPointIds.Any())
					return Enumerable.Empty<Trip>();

				var tripRepo = _databaseFactory.GetRepositoryByType<ITripRepository>(DatabaseType.MongoDb);
				var allTrips = await tripRepo.GetUpcomingTripsAsync(fromDate, days);
				var tripsList = allTrips.ToList();

				var filteredTrips = tripsList.Where(trip => 
					trip.Stops != null && 
					trip.Stops.Any(stop => pickupPointIds.Contains(stop.PickupPointId))
				).ToList();

				foreach (var trip in filteredTrips)
				{
					await PopulateTripSnapshotsAsync(trip);
					await PopulateStopsWithPickupPointNamesAsync(trip);
					FilterTripForParent(trip, studentIds, pickupPointIds);
				}

				foreach (var trip in filteredTrips)
				{
					if (trip.Vehicle != null && trip.VehicleId != Guid.Empty)
					{
						try
						{
							var vehicle = await _vehicleRepository.FindAsync(trip.VehicleId);
							if (vehicle != null && vehicle.HashedLicensePlate != null && vehicle.HashedLicensePlate.Length > 0)
							{
								trip.Vehicle.MaskedPlate = SecurityHelper.DecryptFromBytes(vehicle.HashedLicensePlate);
							}
						}
						catch (Exception ex)
						{
							_logger.LogWarning(ex, "Error decrypting vehicle plate for trip {TripId}", trip.Id);
						}
					}
				}

				return filteredTrips;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting trips by schedule for parent: {ParentEmail}", parentEmail);
				throw;
			}
		}

		public async Task<IEnumerable<Trip>> GetTripsByDateForParentAsync(string parentEmail, DateTime? date = null)
		{
			try
			{
				var targetDate = (date ?? DateTime.UtcNow).Date;

				var students = await _studentRepository.GetStudentsByParentEmailAsync(parentEmail);
				
				if (!students.Any())
					return Enumerable.Empty<Trip>();

				var studentIds = students.Select(s => s.Id).ToList();
				var pickupPointIds = await GetPickupPointIdsForStudentsAsync(studentIds);

				if (!pickupPointIds.Any())
					return Enumerable.Empty<Trip>();

				var tripRepo = _databaseFactory.GetRepositoryByType<ITripRepository>(DatabaseType.MongoDb);
				var allTrips = await tripRepo.GetTripsByDateAsync(targetDate);
				var tripsList = allTrips.ToList();

				var filteredTrips = tripsList.Where(trip => 
					trip.Stops != null && 
					trip.Stops.Any(stop => pickupPointIds.Contains(stop.PickupPointId))
				).ToList();

				foreach (var trip in filteredTrips)
				{
					await PopulateTripSnapshotsAsync(trip);
					await PopulateStopsWithPickupPointNamesAsync(trip);
					FilterTripForParent(trip, studentIds, pickupPointIds);
				}

				foreach (var trip in filteredTrips)
				{
					if (trip.Vehicle != null && trip.VehicleId != Guid.Empty)
					{
						try
						{
							var vehicle = await _vehicleRepository.FindAsync(trip.VehicleId);
							if (vehicle != null && vehicle.HashedLicensePlate != null && vehicle.HashedLicensePlate.Length > 0)
							{
								trip.Vehicle.MaskedPlate = SecurityHelper.DecryptFromBytes(vehicle.HashedLicensePlate);
							}
						}
						catch (Exception ex)
						{
							_logger.LogWarning(ex, "Error decrypting vehicle plate for trip {TripId}", trip.Id);
						}
					}
				}

				return filteredTrips;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting trips by date for parent: {ParentEmail}, {Date}", parentEmail, date);
				throw;
			}
		}

		public async Task<Trip?> GetTripDetailForParentAsync(Guid tripId, string parentEmail)
		{
			try
			{
				var students = await _studentRepository.GetStudentsByParentEmailAsync(parentEmail);
				
				if (!students.Any())
					return null;

				var studentIds = students.Select(s => s.Id).ToList();
				var pickupPointIds = await GetPickupPointIdsForStudentsAsync(studentIds);

				if (!pickupPointIds.Any())
					return null;

				var tripRepo = _databaseFactory.GetRepositoryByType<ITripRepository>(DatabaseType.MongoDb);
				var trip = await tripRepo.FindAsync(tripId);
				
				if (trip == null || trip.IsDeleted)
					return null;

				var hasAccess = trip.Stops != null && 
					trip.Stops.Any(stop => pickupPointIds.Contains(stop.PickupPointId));

				if (!hasAccess)
					return null;

				await PopulateTripSnapshotsAsync(trip);
				await PopulateStopsWithPickupPointNamesAsync(trip);
				FilterTripForParent(trip, studentIds, pickupPointIds);

				if (trip.Vehicle != null && trip.VehicleId != Guid.Empty)
				{
					try
					{
						var vehicle = await _vehicleRepository.FindAsync(trip.VehicleId);
						if (vehicle != null && vehicle.HashedLicensePlate != null && vehicle.HashedLicensePlate.Length > 0)
						{
							trip.Vehicle.MaskedPlate = SecurityHelper.DecryptFromBytes(vehicle.HashedLicensePlate);
						}
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "Error decrypting vehicle plate for trip {TripId}", trip.Id);
					}
				}

				return trip;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting trip detail for parent: {TripId}, {ParentEmail}", tripId, parentEmail);
				throw;
			}
		}

		public async Task<Trip.VehicleLocation?> GetTripCurrentLocationAsync(Guid tripId, string parentEmail)
		{
			try
			{
				var students = await _studentRepository.GetStudentsByParentEmailAsync(parentEmail);
				
				if (!students.Any())
					return null;

				var studentIds = students.Select(s => s.Id).ToList();
				var pickupPointIds = await GetPickupPointIdsForStudentsAsync(studentIds);

				if (!pickupPointIds.Any())
					return null;

				var tripRepo = _databaseFactory.GetRepositoryByType<ITripRepository>(DatabaseType.MongoDb);
				var trip = await tripRepo.FindAsync(tripId);
				
				if (trip == null || trip.IsDeleted)
					return null;

				var hasAccess = trip.Stops != null && 
					trip.Stops.Any(stop => pickupPointIds.Contains(stop.PickupPointId));

				if (!hasAccess)
					return null;

				return trip.CurrentLocation;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting trip current location for parent: {TripId}, {ParentEmail}", tripId, parentEmail);
				throw;
			}
		}
        public async Task<IEnumerable<Guid>> GetParentsForPickupPointAsync(Guid tripId, Guid pickupPointId)
        {
            try
            {
                var tripRepo = _databaseFactory.GetRepositoryByType<ITripRepository>(DatabaseType.MongoDb);
                var trip = await tripRepo.FindAsync(tripId);

                if (trip == null || trip.IsDeleted || trip.Stops == null)
                    return Enumerable.Empty<Guid>();

                // Find the stop with this pickup point
                var stop = trip.Stops.FirstOrDefault(s => s.PickupPointId == pickupPointId);
                if (stop == null || stop.Attendance == null || !stop.Attendance.Any())
                    return Enumerable.Empty<Guid>();

                // Get student IDs from attendance
                var studentIds = stop.Attendance
                    .Where(a => a.StudentId != Guid.Empty)
                    .Select(a => a.StudentId)
                    .Distinct()
                    .ToList();

                if (!studentIds.Any())
                    return Enumerable.Empty<Guid>();

                // Get students and their parent IDs
                var parentIds = new HashSet<Guid>();
                foreach (var studentId in studentIds)
                {
                    var student = await _studentRepository.FindAsync(studentId);
                    if (student != null && !student.IsDeleted && student.ParentId.HasValue)
                    {
                        parentIds.Add(student.ParentId.Value);
                    }
                }

                return parentIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting parents for pickup point {PickupPointId} in trip {TripId}", pickupPointId, tripId);
                return Enumerable.Empty<Guid>();
            }
        }
        public async Task ConfirmArrivalAtStopAsync(Guid tripId, Guid stopId, Guid driverId)
        {
            var tripRepo = _databaseFactory.GetRepositoryByType<ITripRepository>(DatabaseType.MongoDb);
            var trip = await tripRepo.FindAsync(tripId);

            if (trip == null || trip.IsDeleted)
                throw new ArgumentException("Trip not found");

            // Verify driver owns this trip
            if (trip.Driver?.Id != driverId)
                throw new ArgumentException("You don't have access to this trip");

            // Find the stop
            var stop = trip.Stops?.FirstOrDefault(s => s.PickupPointId == stopId);
            if (stop == null)
                throw new ArgumentException("Stop not found in this trip");

			// Check if already arrived
			if (stop.ArrivedAt != null)
				throw new InvalidOperationException("Already confirmed arrival at this stop");

			// Verify vehicle location using VietMapService
			if (trip.CurrentLocation == null || stop.Location == null)
                throw new InvalidOperationException("Vehicle location not available. Please ensure location tracking is enabled");
            var distance = await _vietMapService.CalculateDistanceAsync(
                trip.CurrentLocation.Latitude,
                trip.CurrentLocation.Longitude,
                stop.Location.Latitude,
                stop.Location.Longitude
            );

            if (distance == null)
            {
                _logger.LogWarning("Failed to calculate distance for trip {TripId}, stop {StopId}", tripId, stopId);
                throw new InvalidOperationException("Unable to verify vehicle location. Please try again");
            }

            if (distance > ARRIVAL_THRESHOLD_KM)
            {
                var distanceMeters = Math.Round(distance.Value * 1000);
                _logger.LogWarning(
                    "Driver {DriverId} attempted to confirm arrival at stop {StopId}, but vehicle is {Distance} km away (threshold: {Threshold} km)",
                    driverId, stopId, distance, ARRIVAL_THRESHOLD_KM);
                throw new InvalidOperationException($"Vehicle is {distanceMeters} meters away. Please move closer to the pickup point");
            }

            // Update arrival time
            stop.ArrivedAt = DateTime.UtcNow;
            await tripRepo.UpdateAsync(trip);

            // Get parents for this pickup point
            var parentIds = await GetParentsForPickupPointAsync(tripId, stopId);

            // Create notification for each parent
            foreach (var parentId in parentIds)
            {
                var notificationDto = new CreateNotificationDto
                {
                    UserId = parentId,
                    Title = "Driver Arrived at Pickup Point",
                    Message = "The driver has arrived at the pickup point for your child.",
                    NotificationType = NotificationType.TripInfo,
                    RecipientType = RecipientType.Parent,
                    Priority = 2,
                    RelatedEntityId = tripId,
                    RelatedEntityType = "Trip",
                    ActionRequired = false,
                    ActionUrl = $"/trip/{tripId}",
                    Metadata = new Dictionary<string, object>
                    {
                        { "tripId", tripId.ToString() },
                        { "stopId", stopId.ToString() },
                        { "stopName", stop.Location?.Address ?? "Unknown Stop" },
                        { "driverName", trip.Driver?.FullName ?? "Driver" },
                        { "arrivedAt", stop.ArrivedAt?.ToString("O") ?? DateTime.UtcNow.ToString("O") }
                    }
                };

                // Create notification (automatically sends real-time via SignalR)
                await _notificationService.CreateNotificationAsync(notificationDto);
            }

            _logger.LogInformation(
                "Driver {DriverId} confirmed arrival at stop {StopId} for trip {TripId}. Vehicle distance: {Distance} km",
                driverId, stopId, tripId, distance);
        }

		public async Task<bool> ArrangeStopSequenceAsync(Guid tripId, Guid driverId, Guid pickupPointId, int newSequenceOrder)
		{
			try
			{
				var tripRepo = _databaseFactory.GetRepositoryByType<ITripRepository>(DatabaseType.MongoDb);
				var trip = await tripRepo.FindAsync(tripId);

				if (trip == null || trip.IsDeleted)
				{
					_logger.LogWarning("Trip {TripId} not found", tripId);
					return false;
				}

				// Verify driver owns this trip
				if (trip.Driver?.Id != driverId)
				{
					_logger.LogWarning("Driver {DriverId} does not have access to trip {TripId}", driverId, tripId);
					return false;
				}

				// Verify trip is in progress
				if (trip.Status != TripStatus.InProgress)
				{
					_logger.LogWarning("Cannot arrange stops for trip {TripId} with status {Status}", tripId, trip.Status);
					throw new InvalidOperationException($"Cannot arrange stops. Trip must be in progress (current status: {trip.Status})");
				}

				if (trip.Stops == null || !trip.Stops.Any())
				{
					_logger.LogWarning("Trip {TripId} has no stops", tripId);
					return false;
				}

				// STEP 1: Normalize sequence orders to 0-based index
				var orderedStops = trip.Stops.OrderBy(s => s.SequenceOrder).ToList();
				for (int i = 0; i < orderedStops.Count; i++)
				{
					orderedStops[i].SequenceOrder = i;
				}

				// Find the stop to move
				var stopToMove = trip.Stops.FirstOrDefault(s => s.PickupPointId == pickupPointId);
				if (stopToMove == null)
				{
					_logger.LogWarning("Stop with PickupPointId {PickupPointId} not found in trip {TripId}", pickupPointId, tripId);
					throw new ArgumentException("Stop not found in this trip");
				}

				// Validate the stop hasn't been passed yet (ArrivedAt is null)
				if (stopToMove.ArrivedAt.HasValue)
				{
					_logger.LogWarning("Cannot move stop {PickupPointId} that has already been passed (arrived at {ArrivedAt})",
						pickupPointId, stopToMove.ArrivedAt);
					throw new InvalidOperationException("Cannot rearrange a stop that has already been passed");
				}

				// Validate new sequence order is within valid range (0-based)
				if (newSequenceOrder < 0 || newSequenceOrder >= trip.Stops.Count)
				{
					throw new ArgumentException($"New sequence order must be between 0 and {trip.Stops.Count - 1}");
				}

				// Get all passed stops (those with ArrivedAt set)
				var passedStops = trip.Stops.Where(s => s.ArrivedAt.HasValue).OrderBy(s => s.SequenceOrder).ToList();

				// Find the highest sequence order among passed stops
				var maxPassedSequence = passedStops.Any() ? passedStops.Max(s => s.SequenceOrder) : -1;

				// Validate that new position doesn't break the order of passed stops
				if (newSequenceOrder <= maxPassedSequence)
				{
					_logger.LogWarning("Cannot move stop to sequence {NewSequence} because stops up to sequence {MaxPassed} have already been passed",
						newSequenceOrder, maxPassedSequence);
					throw new InvalidOperationException($"Cannot move stop to position {newSequenceOrder}. Stops up to position {maxPassedSequence} have already been passed");
				}

				var currentSequence = stopToMove.SequenceOrder;

				// If no change needed
				if (currentSequence == newSequenceOrder)
				{
					_logger.LogInformation("Stop {PickupPointId} is already at sequence {Sequence}", pickupPointId, newSequenceOrder);
					return true;
				}

				// Reorder stops
				if (currentSequence < newSequenceOrder)
				{
					// Moving down: shift stops between current and new position up
					foreach (var stop in trip.Stops.Where(s => s.SequenceOrder > currentSequence && s.SequenceOrder <= newSequenceOrder))
					{
						stop.SequenceOrder--;
					}
				}
				else
				{
					// Moving up: shift stops between new and current position down
					foreach (var stop in trip.Stops.Where(s => s.SequenceOrder >= newSequenceOrder && s.SequenceOrder < currentSequence))
					{
						stop.SequenceOrder++;
					}
				}

				// Update the moved stop's sequence
				stopToMove.SequenceOrder = newSequenceOrder;

				// Save changes
				await tripRepo.UpdateAsync(trip);

				_logger.LogInformation("Driver {DriverId} rearranged stop {PickupPointId} from sequence {OldSequence} to {NewSequence} in trip {TripId}",
					driverId, pickupPointId, currentSequence, newSequenceOrder, tripId);

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error arranging stop sequence for trip {TripId}", tripId);
				throw;
			}
		}

		public async Task<bool> UpdateMultipleStopsSequenceAsync(Guid tripId, Guid driverId, List<(Guid PickupPointId, int SequenceOrder)> stopSequences)
		{
			try
			{
				var tripRepo = _databaseFactory.GetRepositoryByType<ITripRepository>(DatabaseType.MongoDb);
				var trip = await tripRepo.FindAsync(tripId);

				if (trip == null || trip.IsDeleted)
				{
					_logger.LogWarning("Trip {TripId} not found", tripId);
					return false;
				}

				// Verify driver owns this trip
				if (trip.Driver?.Id != driverId)
				{
					_logger.LogWarning("Driver {DriverId} does not have access to trip {TripId}", driverId, tripId);
					return false;
				}

				// Verify trip is in progress
				if (trip.Status != TripStatus.InProgress)
				{
					_logger.LogWarning("Cannot arrange stops for trip {TripId} with status {Status}", tripId, trip.Status);
					throw new InvalidOperationException($"Cannot arrange stops. Trip must be in progress (current status: {trip.Status})");
				}

				if (trip.Stops == null || !trip.Stops.Any())
				{
					_logger.LogWarning("Trip {TripId} has no stops", tripId);
					return false;
				}

				if (stopSequences == null || !stopSequences.Any())
				{
					throw new ArgumentException("Stop sequences list cannot be empty");
				}

				// STEP 1: Normalize sequence orders to 0-based index
				var orderedStops = trip.Stops.OrderBy(s => s.SequenceOrder).ToList();
				for (int i = 0; i < orderedStops.Count; i++)
				{
					orderedStops[i].SequenceOrder = i;
				}

				// Validate all pickup points exist in the trip
				var tripPickupPointIds = trip.Stops.Select(s => s.PickupPointId).ToHashSet();
				var requestPickupPointIds = stopSequences.Select(s => s.PickupPointId).ToHashSet();

				var missingPickupPoints = requestPickupPointIds.Except(tripPickupPointIds).ToList();
				if (missingPickupPoints.Any())
				{
					throw new ArgumentException($"The following pickup points are not in this trip: {string.Join(", ", missingPickupPoints)}");
				}

				// Validate all stops in the request haven't been passed yet
				var stopsToUpdate = trip.Stops.Where(s => requestPickupPointIds.Contains(s.PickupPointId)).ToList();
				var passedStopsInRequest = stopsToUpdate.Where(s => s.ArrivedAt.HasValue).ToList();

				if (passedStopsInRequest.Any())
				{
					var passedStopIds = string.Join(", ", passedStopsInRequest.Select(s => s.PickupPointId));
					_logger.LogWarning("Cannot update sequence for passed stops: {PassedStops}", passedStopIds);
					throw new InvalidOperationException($"Cannot rearrange stops that have already been passed: {passedStopIds}");
				}

				// Get all passed stops (those with ArrivedAt set)
				var passedStops = trip.Stops.Where(s => s.ArrivedAt.HasValue).OrderBy(s => s.SequenceOrder).ToList();
				var maxPassedSequence = passedStops.Any() ? passedStops.Max(s => s.SequenceOrder) : -1;

				// Validate sequence orders are valid and don't conflict with passed stops
				var newSequenceOrders = stopSequences.Select(s => s.SequenceOrder).ToList();

				// Check for duplicate sequence orders in the request
				if (newSequenceOrders.Count != newSequenceOrders.Distinct().Count())
				{
					throw new ArgumentException("Duplicate sequence orders found in the request");
				}

				// Check if any new sequence order is in the passed range
				var invalidSequences = newSequenceOrders.Where(seq => seq <= maxPassedSequence).ToList();
				if (invalidSequences.Any())
				{
					_logger.LogWarning("Cannot assign sequences {InvalidSequences} because stops up to sequence {MaxPassed} have already been passed",
						string.Join(", ", invalidSequences), maxPassedSequence);
					throw new InvalidOperationException($"Cannot assign sequence orders {string.Join(", ", invalidSequences)}. Stops up to position {maxPassedSequence} have already been passed");
				}

				// Check if sequence orders are within valid range (0-based)
				var minSequence = newSequenceOrders.Min();
				var maxSequence = newSequenceOrders.Max();

				if (minSequence < 0 || maxSequence >= trip.Stops.Count)
				{
					throw new ArgumentException($"Sequence orders must be between 0 and {trip.Stops.Count - 1}");
				}

				// Create a mapping of PickupPointId to new sequence order
				var sequenceMap = stopSequences.ToDictionary(s => s.PickupPointId, s => s.SequenceOrder);

				// Build a list of all stops with their new or existing sequence orders
				var allStopsWithNewSequence = trip.Stops.Select(stop => new
				{
					Stop = stop,
					NewSequence = sequenceMap.ContainsKey(stop.PickupPointId) ? sequenceMap[stop.PickupPointId] : stop.SequenceOrder,
					IsUpdated = sequenceMap.ContainsKey(stop.PickupPointId)
				}).ToList();

				// Sort by new sequence to detect conflicts
				var sortedStops = allStopsWithNewSequence.OrderBy(s => s.NewSequence).ToList();

				// Reassign sequence orders to ensure no gaps or duplicates
				// Strategy: Keep passed stops in their positions, then arrange unpassed stops
				var finalSequence = 0;
				var stopSequenceAssignments = new Dictionary<Guid, int>();

				// First, preserve passed stops' positions
				foreach (var passedStop in passedStops)
				{
					stopSequenceAssignments[passedStop.PickupPointId] = passedStop.SequenceOrder;
					finalSequence = Math.Max(finalSequence, passedStop.SequenceOrder + 1);
				}

				// Then, assign new sequences to unpassed stops based on the request
				var unpassedStopsOrdered = sortedStops
					.Where(s => !s.Stop.ArrivedAt.HasValue)
					.OrderBy(s => s.NewSequence)
					.ToList();

				foreach (var stopInfo in unpassedStopsOrdered)
				{
					stopSequenceAssignments[stopInfo.Stop.PickupPointId] = finalSequence++;
				}

				// Apply the new sequence orders
				foreach (var stop in trip.Stops)
				{
					if (stopSequenceAssignments.ContainsKey(stop.PickupPointId))
					{
						stop.SequenceOrder = stopSequenceAssignments[stop.PickupPointId];
					}
				}

				// Save changes
				await tripRepo.UpdateAsync(trip);

				_logger.LogInformation("Driver {DriverId} updated sequence for {Count} stops in trip {TripId}",
					driverId, stopSequences.Count, tripId);

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating multiple stops sequence for trip {TripId}", tripId);
				throw;
			}
		}

		private async Task<List<Guid>> GetPickupPointIdsForStudentsAsync(List<Guid> studentIds)
		{
			try
			{
				
				var pickupPointIds = new HashSet<Guid>();
				var now = DateTime.UtcNow;

				foreach (var studentId in studentIds)
				{
					var student = await _studentRepository.FindAsync(studentId);
					if (student != null && !student.IsDeleted)
					{
						if (student.CurrentPickupPointId.HasValue)
						{
							pickupPointIds.Add(student.CurrentPickupPointId.Value);
						}

						var allHistory = await _studentPickupPointHistoryRepository.FindByConditionAsync(
							h => h.StudentId == studentId && 
							h.AssignedAt <= now && 
							(h.RemovedAt == null || h.RemovedAt > now) &&
							!h.IsDeleted
						);

						foreach (var history in allHistory)
						{
							pickupPointIds.Add(history.PickupPointId);
						}
					}
				}

				return pickupPointIds.ToList();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting pickup point IDs for students");
				throw;
			}
		}

		private void FilterTripForParent(Trip trip, List<Guid> studentIds, List<Guid> pickupPointIds)
		{
			if (trip.Stops == null)
				return;

			var filteredStops = trip.Stops
				.Where(stop => pickupPointIds.Contains(stop.PickupPointId))
				.Select(stop => new TripStop
				{
					SequenceOrder = stop.SequenceOrder,
					PickupPointId = stop.PickupPointId,
					PlannedAt = stop.PlannedAt,
					ArrivedAt = stop.ArrivedAt,
					DepartedAt = stop.DepartedAt,
					Location = stop.Location,
					Attendance = stop.Attendance?.Where(a => studentIds.Contains(a.StudentId)).ToList() ?? new List<Attendance>()
				})
				.OrderBy(s => s.SequenceOrder)
				.ToList();

			trip.Stops = filteredStops;
		}

	}
}
