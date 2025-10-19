using Data.Models;
using Services.Models.Route;


namespace Services.Contracts
{
	public interface IRouteScheduleService
	{
		Task<IEnumerable<RouteSchedule>> GetAllRouteSchedulesAsync();
		Task<RouteSchedule?> GetRouteScheduleByIdAsync(Guid id);
		Task<RouteSchedule> CreateRouteScheduleAsync(RouteSchedule routeSchedule);
		Task<RouteSchedule?> UpdateRouteScheduleAsync(RouteSchedule routeSchedule);
		Task<RouteSchedule?> DeleteRouteScheduleAsync(Guid id);
		Task<IEnumerable<RouteSchedule>> GetActiveRouteSchedulesAsync();
		Task<IEnumerable<RouteSchedule>> GetRouteSchedulesByRouteAsync(Guid routeId);
		Task<IEnumerable<RouteSchedule>> GetRouteSchedulesByScheduleAsync(Guid scheduleId);
		Task<IEnumerable<RouteSchedule>> GetRouteSchedulesInDateRangeAsync(DateTime startDate, DateTime endDate);
		Task<bool> RouteScheduleExistsAsync(Guid id);
		Task<IEnumerable<RouteSchedule>> QueryRouteSchedulesAsync(
			Guid? routeId,
			Guid? scheduleId,
			DateTime? startDate,
			DateTime? endDate,
			bool? activeOnly,
			int page,
			int perPage,
			string sortBy,
			string sortOrder);

		Task<bool> DeactivateRouteSchedulesByRouteAsync(Guid routeId);
		Task<bool> DeactivateRouteSchedulesByScheduleAsync(Guid scheduleId);
		Task<IEnumerable<RouteSchedule>> GetOrphanedRouteSchedulesAsync();
		Task<bool> CleanupOrphanedRouteSchedulesAsync();
		Task<bool> ValidateRouteScheduleIntegrityAsync(Guid routeScheduleId);

		Task<RouteSchedule> ApplySmartDefaultsFromRequestAsync(RouteScheduleRequest request, Guid routeId);
	}
}
