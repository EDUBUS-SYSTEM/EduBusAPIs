using Data.Models;
using Data.Repos.Interfaces;
using MongoDB.Driver;

namespace Data.Repos.MongoDB
{
    public class PickupPointResetLogRepository : MongoRepository<PickupPointResetLog>, IMongoRepository<PickupPointResetLog>
    {
        public PickupPointResetLogRepository(IMongoDatabase database) : base(database, "pickupPointResetLogs")
        {
        }
    }
}

