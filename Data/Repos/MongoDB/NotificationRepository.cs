using Data.Models;
using Data.Repos.Interfaces;
using MongoDB.Driver;

namespace Data.Repos.MongoDB
{
    public class NotificationRepository : MongoRepository<Notification>, IMongoRepository<Notification>
    {
        public NotificationRepository(IMongoDatabase database) : base(database, "notifications")
        {
        }
    }
}
