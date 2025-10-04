using Data.Contexts.MongoDB;
using Data.Models;
using Data.Repos.Interfaces;
using MongoDB.Driver;

namespace Data.Repos.MongoDB
{
	public class RouteScheduleRepository : MongoRepository<RouteSchedule>, IRouteScheduleRepository
	{
		public RouteScheduleRepository(IMongoDatabase database) : base(database, "routeschedules")
		{
		}

		public async Task<IEnumerable<RouteSchedule>> GetActiveRouteSchedulesAsync()
		{
			var filter = Builders<RouteSchedule>.Filter.Eq(rs => rs.IsActive, true) &
						Builders<RouteSchedule>.Filter.Eq(rs => rs.IsDeleted, false);
			return await FindByFilterAsync(filter);
		}

		public async Task<IEnumerable<RouteSchedule>> GetRouteSchedulesByRouteAsync(Guid routeId)
		{
			var filter = Builders<RouteSchedule>.Filter.Eq(rs => rs.RouteId, routeId) &
						Builders<RouteSchedule>.Filter.Eq(rs => rs.IsDeleted, false);
			return await FindByFilterAsync(filter);
		}

		public async Task<IEnumerable<RouteSchedule>> GetRouteSchedulesByScheduleAsync(Guid scheduleId)
		{
			var filter = Builders<RouteSchedule>.Filter.Eq(rs => rs.ScheduleId, scheduleId) &
						Builders<RouteSchedule>.Filter.Eq(rs => rs.IsDeleted, false);
			return await FindByFilterAsync(filter);
		}

		public async Task<IEnumerable<RouteSchedule>> GetRouteSchedulesInDateRangeAsync(DateTime startDate, DateTime endDate)
		{
			var filter = Builders<RouteSchedule>.Filter.And(
				Builders<RouteSchedule>.Filter.Eq(rs => rs.IsDeleted, false),
				Builders<RouteSchedule>.Filter.Lte(rs => rs.EffectiveFrom, endDate),
				Builders<RouteSchedule>.Filter.Or(
					Builders<RouteSchedule>.Filter.Eq(rs => rs.EffectiveTo, null),
					Builders<RouteSchedule>.Filter.Gte(rs => rs.EffectiveTo, startDate)
				)
			);
			return await FindByFilterAsync(filter);
		}
	}
}