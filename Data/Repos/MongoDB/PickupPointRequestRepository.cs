using Data.Repos.Interfaces;
using MongoDB.Driver;

namespace Data.Repos.MongoDB
{
    public class PickupPointRequestRepository : MongoRepository<PickupPointRequestDocument>, IPickupPointRequestRepository
    {
        public PickupPointRequestRepository(IMongoDatabase db)
            : base(db, "pickuppointrequestdocument") { }


        public async Task<List<PickupPointRequestDocument>> QueryAsync(string? status, string? parentEmail, int skip, int take)
        {
            var filter = Builders<PickupPointRequestDocument>.Filter.Eq(x => x.IsDeleted, false);
            if (!string.IsNullOrWhiteSpace(status))
                filter &= Builders<PickupPointRequestDocument>.Filter.Eq(x => x.Status, status);
            if (!string.IsNullOrWhiteSpace(parentEmail))
                filter &= Builders<PickupPointRequestDocument>.Filter.Eq(x => x.ParentEmail, parentEmail);

            var sort = Builders<PickupPointRequestDocument>.Sort.Descending(x => x.CreatedAt);
            var res = await _collection.Find(filter).Sort(sort).Skip(skip).Limit(take).ToListAsync();
            return res;
        }
    }
}
