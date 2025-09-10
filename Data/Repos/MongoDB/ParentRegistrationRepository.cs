using Data.Models;
using Data.Repos.Interfaces;
using MongoDB.Driver;

namespace Data.Repos.MongoDB
{
    public class ParentRegistrationRepository : MongoRepository<ParentRegistrationDocument>, IParentRegistrationRepository
    {
        public ParentRegistrationRepository(IMongoDatabase database) 
            : base(database, "ParentRegistrations")
        {
        }


        public async Task<ParentRegistrationDocument?> FindByEmailAsync(string email)
        {
            var filter = Builders<ParentRegistrationDocument>.Filter.And(
                Builders<ParentRegistrationDocument>.Filter.Eq(x => x.Email, email),
                Builders<ParentRegistrationDocument>.Filter.Eq(x => x.Status, "Pending"),
                Builders<ParentRegistrationDocument>.Filter.Eq(x => x.IsDeleted, false)
            );
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<List<ParentRegistrationDocument>> GetExpiredRegistrationsAsync()
        {
            var filter = Builders<ParentRegistrationDocument>.Filter.And(
                Builders<ParentRegistrationDocument>.Filter.Lt(x => x.ExpiresAt, DateTime.UtcNow),
                Builders<ParentRegistrationDocument>.Filter.Eq(x => x.Status, "Pending"),
                Builders<ParentRegistrationDocument>.Filter.Eq(x => x.IsDeleted, false)
            );
            return await _collection.Find(filter).ToListAsync();
        }

        public async Task CleanupExpiredRegistrationsAsync()
        {
            var filter = Builders<ParentRegistrationDocument>.Filter.And(
                Builders<ParentRegistrationDocument>.Filter.Lt(x => x.ExpiresAt, DateTime.UtcNow),
                Builders<ParentRegistrationDocument>.Filter.Eq(x => x.Status, "Pending"),
                Builders<ParentRegistrationDocument>.Filter.Eq(x => x.IsDeleted, false)
            );
            var update = Builders<ParentRegistrationDocument>.Update
                .Set(x => x.IsDeleted, true)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);
            
            await _collection.UpdateManyAsync(filter, update);
        }
    }
}
