using Data.Models;
using Data.Repos.Interfaces;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Services.Contracts;
using Services.Models.Route;

using Utils;

namespace Services.Implementations
{
	public class RouteScheduleService : IRouteScheduleService
	{
		private readonly IDatabaseFactory _databaseFactory;
		private readonly ILogger<RouteScheduleService> _logger;

		public RouteScheduleService(IDatabaseFactory databaseFactory, ILogger<RouteScheduleService> logger)
		{
			_databaseFactory = databaseFactory;
			_logger = logger;
		}

		public async Task<IEnumerable<RouteSchedule>> QueryRouteSchedulesAsync(
			Guid? routeId,
			Guid? scheduleId,
			DateTime? startDate,
			DateTime? endDate,
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

				var repository = _databaseFactory.GetRepositoryByType<IRouteScheduleRepository>(DatabaseType.MongoDb);

				var filters = new List<FilterDefinition<RouteSchedule>>
		{
			Builders<RouteSchedule>.Filter.Eq(rs => rs.IsDeleted, false)
		};

				if (activeOnly == true)
					filters.Add(Builders<RouteSchedule>.Filter.Eq(rs => rs.IsActive, true));

				if (routeId.HasValue)
					filters.Add(Builders<RouteSchedule>.Filter.Eq(rs => rs.RouteId, routeId.Value));

				if (scheduleId.HasValue)
					filters.Add(Builders<RouteSchedule>.Filter.Eq(rs => rs.ScheduleId, scheduleId.Value));

				if (startDate.HasValue && endDate.HasValue)
				{
					filters.Add(Builders<RouteSchedule>.Filter.And(
						Builders<RouteSchedule>.Filter.Lte(rs => rs.EffectiveFrom, endDate.Value),
						Builders<RouteSchedule>.Filter.Or(
							Builders<RouteSchedule>.Filter.Eq(rs => rs.EffectiveTo, null),
							Builders<RouteSchedule>.Filter.Gte(rs => rs.EffectiveTo, startDate.Value)
						)
					));
				}

				var filter = filters.Count == 1 ? filters[0] : Builders<RouteSchedule>.Filter.And(filters);

				var desc = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
				SortDefinition<RouteSchedule> sort = sortBy?.ToLowerInvariant() switch
				{
					"priority" => desc ? Builders<RouteSchedule>.Sort.Descending(x => x.Priority) : Builders<RouteSchedule>.Sort.Ascending(x => x.Priority),
					"effectiveto" => desc ? Builders<RouteSchedule>.Sort.Descending(x => x.EffectiveTo) : Builders<RouteSchedule>.Sort.Ascending(x => x.EffectiveTo),
					"effectivefrom" or _ => desc ? Builders<RouteSchedule>.Sort.Descending(x => x.EffectiveFrom) : Builders<RouteSchedule>.Sort.Ascending(x => x.EffectiveFrom),
				};

				var skip = (page - 1) * perPage;
				return await repository.FindByFilterAsync(filter, sort, skip, perPage);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error querying route schedules with pagination/sorting");
				throw;
			}
		}

		public async Task<IEnumerable<RouteSchedule>> GetAllRouteSchedulesAsync()
		{
			try
			{
				var repository = _databaseFactory.GetRepositoryByType<IRouteScheduleRepository>(DatabaseType.MongoDb);
				return await repository.FindAllAsync();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting all route schedules");
				throw;
			}
		}

		public async Task<RouteSchedule?> GetRouteScheduleByIdAsync(Guid id)
		{
			try
			{
				var repository = _databaseFactory.GetRepositoryByType<IRouteScheduleRepository>(DatabaseType.MongoDb);
				return await repository.FindAsync(id);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting route schedule with id: {RouteScheduleId}", id);
				throw;
			}
		}

		public async Task<RouteSchedule> CreateRouteScheduleAsync(RouteSchedule routeSchedule)
		{
			try
			{
				if (routeSchedule.RouteId == Guid.Empty)
					throw new ArgumentException("Route ID is required");

				if (routeSchedule.ScheduleId == Guid.Empty)
					throw new ArgumentException("Schedule ID is required");

				await ApplySmartDefaultsAsync(routeSchedule);

				var currentDate = DateTime.UtcNow.Date;
				if (routeSchedule.EffectiveFrom.Date < currentDate)
				{
					throw new ArgumentException($"EffectiveFrom date ({routeSchedule.EffectiveFrom:yyyy-MM-dd}) cannot be in the past. Current date is {currentDate:yyyy-MM-dd}");
				}

				if (routeSchedule.EffectiveTo.HasValue && routeSchedule.EffectiveTo <= routeSchedule.EffectiveFrom)
					throw new ArgumentException("effectiveTo must be greater than effectiveFrom");

				await ValidateRouteAndScheduleExistAsync(routeSchedule.RouteId, routeSchedule.ScheduleId);

				var repository = _databaseFactory.GetRepositoryByType<IRouteScheduleRepository>(DatabaseType.MongoDb);

				// Query possible overlaps in the requested window
				var to = routeSchedule.EffectiveTo ?? DateTime.MaxValue;
				var windowCandidates = await repository.GetRouteSchedulesInDateRangeAsync(routeSchedule.EffectiveFrom, to);

				// Reject if there is any active overlap on the same route with same or higher priority
				var hasInvalidOverlap = windowCandidates.Any(rs =>
					rs.RouteId == routeSchedule.RouteId &&
					rs.IsActive &&
					!(rs.EffectiveTo.HasValue && rs.EffectiveTo < routeSchedule.EffectiveFrom) &&
					rs.Priority >= routeSchedule.Priority &&
					rs.Id != routeSchedule.Id);

				if (hasInvalidOverlap)
					throw new InvalidOperationException("Overlapping active route schedule with same or higher priority exists for this route.");

				// prevent exact duplicate link
				var dupFilter = Builders<RouteSchedule>.Filter.And(
					Builders<RouteSchedule>.Filter.Eq(x => x.IsDeleted, false),
					Builders<RouteSchedule>.Filter.Eq(x => x.RouteId, routeSchedule.RouteId),
					Builders<RouteSchedule>.Filter.Eq(x => x.ScheduleId, routeSchedule.ScheduleId),
					Builders<RouteSchedule>.Filter.Eq(x => x.Priority, routeSchedule.Priority),
					Builders<RouteSchedule>.Filter.Eq(x => x.IsActive, routeSchedule.IsActive),
					Builders<RouteSchedule>.Filter.Eq(x => x.EffectiveFrom, routeSchedule.EffectiveFrom),
					Builders<RouteSchedule>.Filter.Eq(x => x.EffectiveTo, routeSchedule.EffectiveTo)
				);
				var exactDup = await repository.FindByFilterAsync(dupFilter);
				if (exactDup.Any())
					throw new InvalidOperationException("Duplicate route-schedule link already exists.");

				return await repository.AddAsync(routeSchedule);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating route schedule: {@RouteSchedule}", routeSchedule);
				throw;
			}
		}

		public async Task<RouteSchedule?> UpdateRouteScheduleAsync(RouteSchedule routeSchedule)
		{
			try
			{
				var repository = _databaseFactory.GetRepositoryByType<IRouteScheduleRepository>(DatabaseType.MongoDb);
				var existingRouteSchedule = await repository.FindAsync(routeSchedule.Id);
				if (existingRouteSchedule == null)
					return null;

				var currentDate = DateTime.UtcNow.Date;
				if (routeSchedule.EffectiveFrom.Date < currentDate)
				{
					throw new ArgumentException($"EffectiveFrom date ({routeSchedule.EffectiveFrom:yyyy-MM-dd}) cannot be in the past. Current date is {currentDate:yyyy-MM-dd}");
				}

				if (routeSchedule.EffectiveTo.HasValue && routeSchedule.EffectiveTo <= routeSchedule.EffectiveFrom)
					throw new ArgumentException("effectiveTo must be greater than effectiveFrom");

				await ValidateRouteAndScheduleExistAsync(routeSchedule.RouteId, routeSchedule.ScheduleId);

				var to = routeSchedule.EffectiveTo ?? DateTime.MaxValue;
				var windowCandidates = await repository.GetRouteSchedulesInDateRangeAsync(routeSchedule.EffectiveFrom, to);

				var hasInvalidOverlap = windowCandidates.Any(rs =>
					rs.RouteId == routeSchedule.RouteId &&
					rs.IsActive &&
					!(rs.EffectiveTo.HasValue && rs.EffectiveTo < routeSchedule.EffectiveFrom) &&
					rs.Priority >= routeSchedule.Priority &&
					rs.Id != routeSchedule.Id);

				if (hasInvalidOverlap)
					throw new InvalidOperationException("Overlapping active route schedule with same or higher priority exists for this route.");

				// prevent exact duplicate link
				var dupFilter = Builders<RouteSchedule>.Filter.And(
					Builders<RouteSchedule>.Filter.Eq(x => x.IsDeleted, false),
					Builders<RouteSchedule>.Filter.Eq(x => x.RouteId, routeSchedule.RouteId),
					Builders<RouteSchedule>.Filter.Eq(x => x.ScheduleId, routeSchedule.ScheduleId),
					Builders<RouteSchedule>.Filter.Eq(x => x.Priority, routeSchedule.Priority),
					Builders<RouteSchedule>.Filter.Eq(x => x.IsActive, routeSchedule.IsActive),
					Builders<RouteSchedule>.Filter.Eq(x => x.EffectiveFrom, routeSchedule.EffectiveFrom),
					Builders<RouteSchedule>.Filter.Eq(x => x.EffectiveTo, routeSchedule.EffectiveTo)
				);
				var exactDup = await repository.FindByFilterAsync(dupFilter);
				if (exactDup.Any())
					throw new InvalidOperationException("Duplicate route-schedule link already exists.");

				return await repository.UpdateAsync(routeSchedule);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating route schedule: {@RouteSchedule}", routeSchedule);
				throw;
			}
		}

		public async Task<RouteSchedule?> DeleteRouteScheduleAsync(Guid id)
		{
			try
			{
				var repository = _databaseFactory.GetRepositoryByType<IRouteScheduleRepository>(DatabaseType.MongoDb);
				
				var existingRouteSchedule = await repository.FindAsync(id);
				if (existingRouteSchedule == null)
				{
					_logger.LogWarning("RouteSchedule with ID {RouteScheduleId} not found or already deleted", id);
					return null;
				}

				var deletedRouteSchedule = await repository.DeleteAsync(id);
				
				_logger.LogInformation("Successfully deleted RouteSchedule with ID {RouteScheduleId}", id);
				return deletedRouteSchedule;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting route schedule with id: {RouteScheduleId}", id);
				throw;
			}
		}

		public async Task<IEnumerable<RouteSchedule>> GetActiveRouteSchedulesAsync()
		{
			try
			{
				var repository = _databaseFactory.GetRepositoryByType<IRouteScheduleRepository>(DatabaseType.MongoDb);
				return await repository.GetActiveRouteSchedulesAsync();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting active route schedules");
				throw;
			}
		}

		public async Task<IEnumerable<RouteSchedule>> GetRouteSchedulesByRouteAsync(Guid routeId)
		{
			try
			{
				var repository = _databaseFactory.GetRepositoryByType<IRouteScheduleRepository>(DatabaseType.MongoDb);
				return await repository.GetRouteSchedulesByRouteAsync(routeId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting route schedules by route: {RouteId}", routeId);
				throw;
			}
		}

		public async Task<IEnumerable<RouteSchedule>> GetRouteSchedulesByScheduleAsync(Guid scheduleId)
		{
			try
			{
				var repository = _databaseFactory.GetRepositoryByType<IRouteScheduleRepository>(DatabaseType.MongoDb);
				return await repository.GetRouteSchedulesByScheduleAsync(scheduleId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting route schedules by schedule: {ScheduleId}", scheduleId);
				throw;
			}
		}

		public async Task<IEnumerable<RouteSchedule>> GetRouteSchedulesInDateRangeAsync(DateTime startDate, DateTime endDate)
		{
			try
			{
				var repository = _databaseFactory.GetRepositoryByType<IRouteScheduleRepository>(DatabaseType.MongoDb);
				return await repository.GetRouteSchedulesInDateRangeAsync(startDate, endDate);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting route schedules in date range: {StartDate} to {EndDate}", startDate, endDate);
				throw;
			}
		}

		public async Task<bool> RouteScheduleExistsAsync(Guid id)
		{
			try
			{
				var repository = _databaseFactory.GetRepositoryByType<IRouteScheduleRepository>(DatabaseType.MongoDb);
				return await repository.ExistsAsync(id);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error checking if route schedule exists: {RouteScheduleId}", id);
				throw;
			}
		}

		private async Task ApplySmartDefaultsAsync(RouteSchedule routeSchedule)
		{
			// Get Schedule to inherit EffectiveTo
			var scheduleRepo = _databaseFactory.GetRepositoryByType<IMongoRepository<Schedule>>(DatabaseType.MongoDb);
			var schedule = await scheduleRepo.FindAsync(routeSchedule.ScheduleId);
			if (schedule == null)
				return; // Will be caught by validation later

			// Apply smart defaults
			if (routeSchedule.EffectiveFrom == default(DateTime))
			{
				routeSchedule.EffectiveFrom = DateTime.UtcNow.Date;
			}

			// If EffectiveTo is null, inherit from Schedule
			if (!routeSchedule.EffectiveTo.HasValue)
			{
				routeSchedule.EffectiveTo = schedule.EffectiveTo;
			}

			_logger.LogDebug("Applied smart defaults for RouteSchedule: EffectiveFrom={EffectiveFrom}, EffectiveTo={EffectiveTo}", 
				routeSchedule.EffectiveFrom, routeSchedule.EffectiveTo);
		}

		public async Task<RouteSchedule> ApplySmartDefaultsFromRequestAsync(RouteScheduleRequest request, Guid routeId)
		{
			// Get Schedule to inherit EffectiveTo
			var scheduleRepo = _databaseFactory.GetRepositoryByType<IMongoRepository<Schedule>>(DatabaseType.MongoDb);
			var schedule = await scheduleRepo.FindAsync(request.ScheduleId);
			if (schedule == null)
				throw new InvalidOperationException($"Schedule {request.ScheduleId} not found");

			var routeSchedule = new RouteSchedule
			{
				RouteId = routeId,
				ScheduleId = request.ScheduleId,
				Priority = request.Priority,
				IsActive = true,
				// Apply smart defaults
				EffectiveFrom = request.EffectiveFrom ?? DateTime.UtcNow.Date,
				EffectiveTo = request.EffectiveTo ?? schedule.EffectiveTo
			};

			_logger.LogDebug("Applied smart defaults from request: EffectiveFrom={EffectiveFrom}, EffectiveTo={EffectiveTo}", 
				routeSchedule.EffectiveFrom, routeSchedule.EffectiveTo);

			return routeSchedule;
		}

		private async Task ValidateRouteAndScheduleExistAsync(Guid routeId, Guid scheduleId)
		{
			// Validate Route exists and is active
			var routeRepo = _databaseFactory.GetRepositoryByType<IMongoRepository<Route>>(DatabaseType.MongoDb);
			var route = await routeRepo.FindAsync(routeId);
			if (route == null || route.IsDeleted || !route.IsActive)
				throw new InvalidOperationException($"Route {routeId} does not exist or is inactive");

			// Validate Schedule exists and is active  
			var scheduleRepo = _databaseFactory.GetRepositoryByType<IMongoRepository<Schedule>>(DatabaseType.MongoDb);
			var schedule = await scheduleRepo.FindAsync(scheduleId);
			if (schedule == null || schedule.IsDeleted || !schedule.IsActive)
				throw new InvalidOperationException($"Schedule {scheduleId} does not exist or is inactive");
		}

		public async Task<bool> DeactivateRouteSchedulesByRouteAsync(Guid routeId)
		{
			try
			{
				var repository = _databaseFactory.GetRepositoryByType<IRouteScheduleRepository>(DatabaseType.MongoDb);
				
				var filter = Builders<RouteSchedule>.Filter.And(
					Builders<RouteSchedule>.Filter.Eq(rs => rs.RouteId, routeId),
					Builders<RouteSchedule>.Filter.Eq(rs => rs.IsActive, true),
					Builders<RouteSchedule>.Filter.Eq(rs => rs.IsDeleted, false)
				);
				
				var routeSchedules = await repository.FindByFilterAsync(filter);
				if (!routeSchedules.Any())
					return false;

				var updateCount = 0;
				foreach (var rs in routeSchedules)
				{
					rs.IsActive = false;
					rs.UpdatedAt = DateTime.UtcNow;
					await repository.UpdateAsync(rs);
					updateCount++;
				}

				_logger.LogInformation("Deactivated {Count} route schedules for route {RouteId}", updateCount, routeId);
				return updateCount > 0;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deactivating route schedules for route: {RouteId}", routeId);
				throw;
			}
		}

		public async Task<bool> DeactivateRouteSchedulesByScheduleAsync(Guid scheduleId)
		{
			try
			{
				var repository = _databaseFactory.GetRepositoryByType<IRouteScheduleRepository>(DatabaseType.MongoDb);
				
				var filter = Builders<RouteSchedule>.Filter.And(
					Builders<RouteSchedule>.Filter.Eq(rs => rs.ScheduleId, scheduleId),
					Builders<RouteSchedule>.Filter.Eq(rs => rs.IsActive, true),
					Builders<RouteSchedule>.Filter.Eq(rs => rs.IsDeleted, false)
				);
				
				var routeSchedules = await repository.FindByFilterAsync(filter);
				if (!routeSchedules.Any())
					return false;

				var updateCount = 0;
				foreach (var rs in routeSchedules)
				{
					rs.IsActive = false;
					rs.UpdatedAt = DateTime.UtcNow;
					await repository.UpdateAsync(rs);
					updateCount++;
				}

				_logger.LogInformation("Deactivated {Count} route schedules for schedule {ScheduleId}", updateCount, scheduleId);
				return updateCount > 0;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deactivating route schedules for schedule: {ScheduleId}", scheduleId);
				throw;
			}
		}

		public async Task<IEnumerable<RouteSchedule>> GetOrphanedRouteSchedulesAsync()
		{
			try
			{
				var repository = _databaseFactory.GetRepositoryByType<IRouteScheduleRepository>(DatabaseType.MongoDb);
				var routeRepo = _databaseFactory.GetRepositoryByType<IMongoRepository<Route>>(DatabaseType.MongoDb);
				var scheduleRepo = _databaseFactory.GetRepositoryByType<IMongoRepository<Schedule>>(DatabaseType.MongoDb);

				var allRouteSchedules = await repository.FindAllAsync();
				var orphaned = new List<RouteSchedule>();

				foreach (var rs in allRouteSchedules)
				{
					// Check if Route exists and is active
					var route = await routeRepo.FindAsync(rs.RouteId);
					if (route == null || route.IsDeleted || !route.IsActive)
					{
						orphaned.Add(rs);
						continue;
					}

					// Check if Schedule exists and is active
					var schedule = await scheduleRepo.FindAsync(rs.ScheduleId);
					if (schedule == null || schedule.IsDeleted || !schedule.IsActive)
					{
						orphaned.Add(rs);
					}
				}

				return orphaned;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting orphaned route schedules");
				throw;
			}
		}
		public async Task<bool> CleanupOrphanedRouteSchedulesAsync()
		{
			try
			{
				var orphaned = await GetOrphanedRouteSchedulesAsync();
				if (!orphaned.Any())
					return true;

				var repository = _databaseFactory.GetRepositoryByType<IRouteScheduleRepository>(DatabaseType.MongoDb);
				var updateCount = 0;

				foreach (var rs in orphaned)
				{
					rs.IsActive = false;
					rs.UpdatedAt = DateTime.UtcNow;
					await repository.UpdateAsync(rs);
					updateCount++;
				}

				_logger.LogInformation("Cleaned up {Count} orphaned route schedules", updateCount);
				return updateCount > 0;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error cleaning up orphaned route schedules");
				throw;
			}
		}

		public async Task<bool> ValidateRouteScheduleIntegrityAsync(Guid routeScheduleId)
		{
			try
			{
				var repository = _databaseFactory.GetRepositoryByType<IRouteScheduleRepository>(DatabaseType.MongoDb);
				var routeSchedule = await repository.FindAsync(routeScheduleId);
				
				if (routeSchedule == null)
					return false;

				await ValidateRouteAndScheduleExistAsync(routeSchedule.RouteId, routeSchedule.ScheduleId);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error validating route schedule integrity: {RouteScheduleId}", routeScheduleId);
				return false;
			}
		}
	}
}
