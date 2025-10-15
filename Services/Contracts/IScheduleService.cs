using Data.Models;

namespace Services.Contracts
{
	public interface IScheduleService
	{
		Task<IEnumerable<Schedule>> GetAllSchedulesAsync();
		Task<Schedule?> GetScheduleByIdAsync(Guid id);
		Task<Schedule> CreateScheduleAsync(Schedule schedule);
		Task<Schedule?> UpdateScheduleAsync(Schedule schedule, DateTime? clientUpdatedAt = null);
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
		Task<Schedule?> AddTimeOverrideAsync(Guid scheduleId, ScheduleTimeOverride timeOverride);
		Task<Schedule?> AddTimeOverrideAsync(Guid scheduleId, ScheduleTimeOverride timeOverride, DateTime? clientUpdatedAt);
		Task<Schedule?> AddTimeOverridesBatchAsync(Guid scheduleId, List<ScheduleTimeOverride> timeOverrides);
		Task<Schedule?> RemoveTimeOverrideAsync(Guid scheduleId, DateTime date);
		Task<Schedule?> RemoveTimeOverrideAsync(Guid scheduleId, DateTime date, DateTime? clientUpdatedAt);
		Task<Schedule?> RemoveTimeOverridesBatchAsync(Guid scheduleId, List<DateTime> dates);
		Task<List<ScheduleTimeOverride>> GetTimeOverridesAsync(Guid scheduleId);
		Task<ScheduleTimeOverride?> GetTimeOverrideAsync(Guid scheduleId, DateTime date);
		Task<List<DateTime>> GenerateScheduleDatesAsync(Guid scheduleId, DateTime startDate, DateTime endDate);
		Task<bool> IsDateMatchingScheduleAsync(Guid scheduleId, DateTime date);
	}
}
