using Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repos.Interfaces
{
	public interface IScheduleRepository : IMongoRepository<Schedule>
	{
		Task<IEnumerable<Schedule>> GetActiveSchedulesAsync();
		Task<IEnumerable<Schedule>> GetSchedulesByTypeAsync(string scheduleType);
		Task<IEnumerable<Schedule>> GetSchedulesInDateRangeAsync(DateTime startDate, DateTime endDate);
		Task<IEnumerable<Schedule>> GetSchedulesByRouteAsync(Guid routeId);
	}
}
