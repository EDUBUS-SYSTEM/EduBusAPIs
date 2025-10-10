using Data.Models;
using Data.Repos.Interfaces;
using MongoDB.Driver;

namespace Data.Repos.MongoDB
{
    public class RouteRepository : MongoRepository<Route>, IMongoRepository<Route>
    {
        public RouteRepository(IMongoDatabase database) : base(database, "routes")
        {
        }
        public override async Task<Route?> DeleteAsync(Guid id)
        {
            var filter = Builders<Route>.Filter.Eq(x => x.Id, id);
            var update = Builders<Route>.Update
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
