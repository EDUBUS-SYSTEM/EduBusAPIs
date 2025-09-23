using Data.Models;
using Data.Repos.Interfaces;
using MongoDB.Driver;

namespace Data.Repos.MongoDB
{
    public class RouteScheduleRepository : MongoRepository<RouteSchedule>, IMongoRepository<RouteSchedule>
    {
        public RouteScheduleRepository(IMongoDatabase database) : base(database, "routeSchedules")
        {
        }

        public override async Task<RouteSchedule?> DeleteAsync(Guid id)
        {
            var filter = Builders<RouteSchedule>.Filter.Eq(x => x.Id, id);
            var update = Builders<RouteSchedule>.Update
                .Set(x => x.IsDeleted, true)
                .Set(x => x.IsActive, false)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);

            var result = await _collection.UpdateOneAsync(filter, update);
            if (result.ModifiedCount > 0)
            {
                return await _collection.Find(filter).FirstOrDefaultAsync();
            }
            return null;
        }
    }
}