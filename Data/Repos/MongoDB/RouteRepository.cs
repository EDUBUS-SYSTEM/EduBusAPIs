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
    }
}
