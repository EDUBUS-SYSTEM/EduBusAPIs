using Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repos.Interfaces
{
	public interface IRouteScheduleRepository : IMongoRepository<RouteSchedule>
	{
		Task<IEnumerable<RouteSchedule>> GetActiveRouteSchedulesAsync();
		Task<IEnumerable<RouteSchedule>> GetRouteSchedulesByRouteAsync(Guid routeId);
		Task<IEnumerable<RouteSchedule>> GetRouteSchedulesByScheduleAsync(Guid scheduleId);
		Task<IEnumerable<RouteSchedule>> GetRouteSchedulesInDateRangeAsync(DateTime startDate, DateTime endDate);
	}
}
