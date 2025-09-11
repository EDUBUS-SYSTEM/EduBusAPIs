using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Services.Contracts;
using Services.Models.Notification;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace Services.Implementations
{
	public class ScheduleService : IScheduleService
	{
		private readonly IDatabaseFactory _databaseFactory;
		private readonly ILogger<ScheduleService> _logger;

		private readonly INotificationService _notificationService;

		public ScheduleService(IDatabaseFactory databaseFactory, ILogger<ScheduleService> logger, INotificationService notificationService)
		{
			_databaseFactory = databaseFactory;
			_logger = logger;
			_notificationService = notificationService;
		}

		public async Task<IEnumerable<Schedule>> QuerySchedulesAsync(
			string? scheduleType,
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

				var repository = _databaseFactory.GetRepositoryByType<IScheduleRepository>(DatabaseType.MongoDb);

				// Build filter
				var filters = new List<FilterDefinition<Schedule>>
		{
			Builders<Schedule>.Filter.Eq(s => s.IsDeleted, false)
		};

				if (activeOnly == true)
					filters.Add(Builders<Schedule>.Filter.Eq(s => s.IsActive, true));

				if (!string.IsNullOrWhiteSpace(scheduleType))
					filters.Add(Builders<Schedule>.Filter.Eq(s => s.ScheduleType, scheduleType));

				if (startDate.HasValue && endDate.HasValue)
				{
					// Intersect with effective window
					filters.Add(Builders<Schedule>.Filter.And(
						Builders<Schedule>.Filter.Lte(s => s.EffectiveFrom, endDate.Value),
						Builders<Schedule>.Filter.Or(
							Builders<Schedule>.Filter.Eq(s => s.EffectiveTo, null),
							Builders<Schedule>.Filter.Gte(s => s.EffectiveTo, startDate.Value)
						)
					));
				}

				var filter = filters.Count == 1 ? filters[0] : Builders<Schedule>.Filter.And(filters);

				// Build sort
				var desc = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
				SortDefinition<Schedule> sort = sortBy?.ToLowerInvariant() switch
				{
					"effectivefrom" => desc ? Builders<Schedule>.Sort.Descending(x => x.EffectiveFrom) : Builders<Schedule>.Sort.Ascending(x => x.EffectiveFrom),
					"effectiveto" => desc ? Builders<Schedule>.Sort.Descending(x => x.EffectiveTo) : Builders<Schedule>.Sort.Ascending(x => x.EffectiveTo),
					"name" => desc ? Builders<Schedule>.Sort.Descending(x => x.Name) : Builders<Schedule>.Sort.Ascending(x => x.Name),
					"scheduletype" => desc ? Builders<Schedule>.Sort.Descending(x => x.ScheduleType) : Builders<Schedule>.Sort.Ascending(x => x.ScheduleType),
					"createdat" or _ => desc ? Builders<Schedule>.Sort.Descending(x => x.CreatedAt) : Builders<Schedule>.Sort.Ascending(x => x.CreatedAt)
				};

				var skip = (page - 1) * perPage;
				var items = await repository.FindByFilterAsync(filter, sort, skip, perPage);
				return items;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error querying schedules with pagination/sorting");
				throw;
			}
		}

		public async Task<IEnumerable<Schedule>> GetAllSchedulesAsync()
		{
			try
			{
				var repository = _databaseFactory.GetRepositoryByType<IScheduleRepository>(DatabaseType.MongoDb);
				return await repository.FindAllAsync();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting all schedules");
				throw;
			}
		}

		public async Task<Schedule?> GetScheduleByIdAsync(Guid id)
		{
			try
			{
				var repository = _databaseFactory.GetRepositoryByType<IScheduleRepository>(DatabaseType.MongoDb);
				return await repository.FindAsync(id);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting schedule with id: {ScheduleId}", id);
				throw;
			}
		}

		public async Task<Schedule> CreateScheduleAsync(Schedule schedule)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(schedule.Name))
					throw new ArgumentException("Schedule name is required");

				if (string.IsNullOrWhiteSpace(schedule.StartTime))
					throw new ArgumentException("Start time is required");

				if (string.IsNullOrWhiteSpace(schedule.EndTime))
					throw new ArgumentException("End time is required");

				ValidateSchedule(schedule);

				var repository = _databaseFactory.GetRepositoryByType<IScheduleRepository>(DatabaseType.MongoDb);

				var dupFilter = Builders<Schedule>.Filter.And(
					Builders<Schedule>.Filter.Eq(x => x.IsDeleted, false),
					Builders<Schedule>.Filter.Eq(x => x.Name, schedule.Name),
					Builders<Schedule>.Filter.Eq(x => x.StartTime, schedule.StartTime),
					Builders<Schedule>.Filter.Eq(x => x.EndTime, schedule.EndTime),
					Builders<Schedule>.Filter.Eq(x => x.Timezone, schedule.Timezone),
					Builders<Schedule>.Filter.Eq(x => x.RRule, schedule.RRule),
					Builders<Schedule>.Filter.Lte(x => x.EffectiveFrom, schedule.EffectiveTo ?? DateTime.MaxValue),
					Builders<Schedule>.Filter.Or(
						Builders<Schedule>.Filter.Eq(x => x.EffectiveTo, null),
						Builders<Schedule>.Filter.Gte(x => x.EffectiveTo, schedule.EffectiveFrom)
					)
				);
				var existingDup = await repository.FindByFilterAsync(dupFilter);
				if (existingDup.Any())
					throw new InvalidOperationException("A schedule with the same time/rule already exists in the overlapping effective window.");

				return await repository.AddAsync(schedule);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating schedule: {@Schedule}", schedule);
				throw;
			}
		}

		public async Task<Schedule?> UpdateScheduleAsync(Schedule schedule)
		{
			try
			{
				var repository = _databaseFactory.GetRepositoryByType<IScheduleRepository>(DatabaseType.MongoDb);
				var existingSchedule = await repository.FindAsync(schedule.Id);
				if (existingSchedule == null)
					return null;

				if (string.IsNullOrWhiteSpace(schedule.Name))
					throw new ArgumentException("Schedule name is required");

				ValidateSchedule(schedule);

				// prevent duplicates on update (exclude self)
				var dupFilter = Builders<Schedule>.Filter.And(
					Builders<Schedule>.Filter.Eq(x => x.IsDeleted, false),
					Builders<Schedule>.Filter.Ne(x => x.Id, schedule.Id),
					Builders<Schedule>.Filter.Eq(x => x.Name, schedule.Name),
					Builders<Schedule>.Filter.Eq(x => x.StartTime, schedule.StartTime),
					Builders<Schedule>.Filter.Eq(x => x.EndTime, schedule.EndTime),
					Builders<Schedule>.Filter.Eq(x => x.Timezone, schedule.Timezone),
					Builders<Schedule>.Filter.Eq(x => x.RRule, schedule.RRule),
					Builders<Schedule>.Filter.Lte(x => x.EffectiveFrom, schedule.EffectiveTo ?? DateTime.MaxValue),
					Builders<Schedule>.Filter.Or(
						Builders<Schedule>.Filter.Eq(x => x.EffectiveTo, null),
						Builders<Schedule>.Filter.Gte(x => x.EffectiveTo, schedule.EffectiveFrom)
					)
				);
				var existingDup = await repository.FindByFilterAsync(dupFilter);
				if (existingDup.Any())
					throw new InvalidOperationException("A schedule with the same time/rule already exists in the overlapping effective window.");

				var updated = await repository.UpdateAsync(schedule);

				if (updated != null && (
					!string.Equals(existingSchedule.Name, schedule.Name, StringComparison.Ordinal) ||
					!string.Equals(existingSchedule.StartTime, schedule.StartTime, StringComparison.Ordinal) ||
					!string.Equals(existingSchedule.EndTime, schedule.EndTime, StringComparison.Ordinal) ||
					!string.Equals(existingSchedule.RRule, schedule.RRule, StringComparison.Ordinal)))
				{
					await _notificationService.CreateAdminNotificationAsync(new CreateAdminNotificationDto
					{
						Title = "Schedule Change Notification",
						Message = $"Schedule '{schedule.Name}' has been updated.",
						NotificationType = NotificationType.ScheduleChange,
						Priority = 2,
						RelatedEntityId = schedule.Id,
						RelatedEntityType = "Schedule",
						ActionRequired = false,
						ActionUrl = null,
						ExpiresAt = null,
						Metadata = new Dictionary<string, object>
				{
					{ "scheduleId", schedule.Id },
					{ "oldName", existingSchedule.Name },
					{ "newName", schedule.Name },
					{ "oldStartTime", existingSchedule.StartTime },
					{ "newStartTime", schedule.StartTime },
					{ "oldEndTime", existingSchedule.EndTime },
					{ "newEndTime", schedule.EndTime },
					{ "oldRRule", existingSchedule.RRule },
					{ "newRRule", schedule.RRule }
				}
					});
				}

				return updated;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating schedule: {@Schedule}", schedule);
				throw;
			}
		}

		public async Task<Schedule?> DeleteScheduleAsync(Guid id)
		{
			try
			{
				var repository = _databaseFactory.GetRepositoryByType<IScheduleRepository>(DatabaseType.MongoDb);
				return await repository.DeleteAsync(id);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting schedule with id: {ScheduleId}", id);
				throw;
			}
		}

		public async Task<IEnumerable<Schedule>> GetActiveSchedulesAsync()
		{
			try
			{
				var repository = _databaseFactory.GetRepositoryByType<IScheduleRepository>(DatabaseType.MongoDb);
				return await repository.GetActiveSchedulesAsync();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting active schedules");
				throw;
			}
		}

		public async Task<IEnumerable<Schedule>> GetSchedulesByTypeAsync(string scheduleType)
		{
			try
			{
				var repository = _databaseFactory.GetRepositoryByType<IScheduleRepository>(DatabaseType.MongoDb);
				return await repository.GetSchedulesByTypeAsync(scheduleType);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting schedules by type: {ScheduleType}", scheduleType);
				throw;
			}
		}

		public async Task<IEnumerable<Schedule>> GetSchedulesInDateRangeAsync(DateTime startDate, DateTime endDate)
		{
			try
			{
				var repository = _databaseFactory.GetRepositoryByType<IScheduleRepository>(DatabaseType.MongoDb);
				return await repository.GetSchedulesInDateRangeAsync(startDate, endDate);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting schedules in date range: {StartDate} to {EndDate}", startDate, endDate);
				throw;
			}
		}

		public async Task<bool> ScheduleExistsAsync(Guid id)
		{
			try
			{
				var repository = _databaseFactory.GetRepositoryByType<IScheduleRepository>(DatabaseType.MongoDb);
				return await repository.ExistsAsync(id);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error checking if schedule exists: {ScheduleId}", id);
				throw;
			}
		}

		private static void ValidateSchedule(Schedule s)
		{
			// time format HH:mm or HH:mm:ss
			if (!TryParseHms(s.StartTime, out var sh, out var sm, out var ss))
				throw new ArgumentException("StartTime must be HH:mm or HH:mm:ss");
			if (!TryParseHms(s.EndTime, out var eh, out var em, out var es))
				throw new ArgumentException("EndTime must be HH:mm or HH:mm:ss");

			// end could be past midnight; only reject if exactly equal to start
			if (sh == eh && sm == em && ss == es)
				throw new ArgumentException("EndTime must be different from StartTime");

			// timezone
			if (!IsValidTimezone(s.Timezone))
				throw new ArgumentException($"Invalid timezone: {s.Timezone}");

			// effective range
			if (s.EffectiveTo.HasValue && s.EffectiveTo.Value <= s.EffectiveFrom)
				throw new ArgumentException("effectiveTo must be greater than or equal to effectiveFrom");

			// RRULE basic validation (support DAILY or WEEKLY with optional BYDAY)
			ValidateBasicRRule(s.RRule);
		}

		private static bool TryParseHms(string hms, out int h, out int m, out int s)
		{
			h = m = s = 0;
			if (string.IsNullOrWhiteSpace(hms)) return false;
			var parts = hms.Split(':');
			if (parts.Length < 2 || parts.Length > 3) return false;
			if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out h)) return false;
			if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out m)) return false;
			if (parts.Length == 3 && !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out s)) s = 0;
			return h >= 0 && h <= 23 && m >= 0 && m <= 59 && s >= 0 && s <= 59;
		}

		private static bool IsValidTimezone(string timezoneId)
		{
			if (string.IsNullOrWhiteSpace(timezoneId)) return false;
			try
			{
				_ = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
				return true;
			}
			catch
			{
				return false;
			}
		}

		private static void ValidateBasicRRule(string rrule)
		{
			// Allow empty RRULE (treated as DAILY)
			if (string.IsNullOrWhiteSpace(rrule)) return;

			var freq = "";
			HashSet<string> byDays = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			var parts = rrule.Split(';', StringSplitOptions.RemoveEmptyEntries);
			foreach (var p in parts)
			{
				var kv = p.Split('=', 2);
				if (kv.Length != 2) continue;
				var key = kv[0].Trim().ToUpperInvariant();
				var val = kv[1].Trim().ToUpperInvariant();

				if (key == "FREQ")
					freq = val;
				else if (key == "BYDAY")
				{
					foreach (var d in val.Split(',', StringSplitOptions.RemoveEmptyEntries))
						byDays.Add(d.Trim());
				}
			}

			// Only support DAILY or WEEKLY
			if (freq != "DAILY" && freq != "WEEKLY")
				throw new ArgumentException("RRULE FREQ must be DAILY or WEEKLY");

			// If WEEKLY with BYDAY, validate tokens
			if (freq == "WEEKLY" && byDays.Count > 0)
			{
				var valid = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "MO", "TU", "WE", "TH", "FR", "SA", "SU" };
				if (!byDays.All(valid.Contains))
					throw new ArgumentException("RRULE BYDAY must contain only MO,TU,WE,TH,FR,SA,SU");
			}
		}
	}
}
