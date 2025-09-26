using Data.Contexts.MongoDB;
using Data.Models;
using Data.Repos.Interfaces;
using MongoDB.Driver;

namespace Data.Repos.MongoDB
{
	public class ScheduleRepository : MongoRepository<Schedule>, IScheduleRepository
	{
		public ScheduleRepository(IMongoDatabase database) : base(database, "schedules")
		{
		}

		public async Task<IEnumerable<Schedule>> GetActiveSchedulesAsync()
		{
			var filter = Builders<Schedule>.Filter.Eq(s => s.IsActive, true) &
						Builders<Schedule>.Filter.Eq(s => s.IsDeleted, false);
			return await FindByFilterAsync(filter);
		}

		public async Task<IEnumerable<Schedule>> GetSchedulesByTypeAsync(string scheduleType)
		{
			var filter = Builders<Schedule>.Filter.Eq(s => s.ScheduleType, scheduleType) &
						Builders<Schedule>.Filter.Eq(s => s.IsDeleted, false);
			return await FindByFilterAsync(filter);
		}

		public async Task<IEnumerable<Schedule>> GetSchedulesInDateRangeAsync(DateTime startDate, DateTime endDate)
		{
			var filter = Builders<Schedule>.Filter.And(
				Builders<Schedule>.Filter.Eq(s => s.IsDeleted, false),
				Builders<Schedule>.Filter.Lte(s => s.EffectiveFrom, endDate),
				Builders<Schedule>.Filter.Or(
					Builders<Schedule>.Filter.Eq(s => s.EffectiveTo, null),
					Builders<Schedule>.Filter.Gte(s => s.EffectiveTo, startDate)
				)
			);
			return await FindByFilterAsync(filter);
		}

		public async Task<IEnumerable<Schedule>> GetSchedulesByRouteAsync(Guid routeId)
		{
			// This would require joining with RouteSchedule collection
			// For now, return empty - this should be handled in the service layer
			return new List<Schedule>();
		}
	}
}