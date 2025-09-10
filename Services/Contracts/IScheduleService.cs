using Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Contracts
{
	public interface IScheduleService
	{
		Task<IEnumerable<Schedule>> GetAllSchedulesAsync();
		Task<Schedule?> GetScheduleByIdAsync(Guid id);
		Task<Schedule> CreateScheduleAsync(Schedule schedule);
		Task<Schedule?> UpdateScheduleAsync(Schedule schedule);
		Task<Schedule?> DeleteScheduleAsync(Guid id);
		Task<IEnumerable<Schedule>> GetActiveSchedulesAsync();
		Task<IEnumerable<Schedule>> GetSchedulesByTypeAsync(string scheduleType);
		Task<IEnumerable<Schedule>> GetSchedulesInDateRangeAsync(DateTime startDate, DateTime endDate);
		Task<bool> ScheduleExistsAsync(Guid id);
		Task<IEnumerable<Schedule>> QuerySchedulesAsync(
			string? scheduleType,
			DateTime? startDate,
			DateTime? endDate,
			bool? activeOnly,
			int page,
			int perPage,
			string sortBy,
			string sortOrder);
	}
}
