using Data.Contexts.MongoDB;
using Data.Models;
using Data.Repos.Interfaces;
using MongoDB.Driver;

namespace Data.Repos.MongoDB
{
    public class TripLocationHistoryRepository : MongoRepository<TripLocationHistory>, ITripLocationHistoryRepository
    {
        public TripLocationHistoryRepository(IMongoDatabase database)
            : base(database, "trip_location_history")
        {
        }

        public async Task<IEnumerable<TripLocationHistory>> GetLocationHistoryByTripIdAsync(Guid tripId)
        {
            var filter = Builders<TripLocationHistory>.Filter.And(
                Builders<TripLocationHistory>.Filter.Eq(x => x.TripId, tripId),
                Builders<TripLocationHistory>.Filter.Eq(x => x.IsDeleted, false)
            );

            var sort = Builders<TripLocationHistory>.Sort.Descending(x => x.Location.RecordedAt);
            return await FindByFilterAsync(filter, sort);
        }

        public async Task<TripLocationHistory?> GetLatestLocationByTripIdAsync(Guid tripId)
        {
            var filter = Builders<TripLocationHistory>.Filter.And(
                Builders<TripLocationHistory>.Filter.Eq(x => x.TripId, tripId),
                Builders<TripLocationHistory>.Filter.Eq(x => x.IsDeleted, false)
            );

            var sort = Builders<TripLocationHistory>.Sort.Descending(x => x.Location.RecordedAt);
            var results = await FindByFilterAsync(filter, sort, 0, 1);
            return results.FirstOrDefault();
        }

        public async Task<IEnumerable<TripLocationHistory>> GetLocationHistoryByTripIdAndDateRangeAsync(
            Guid tripId,
            DateTime startDate,
            DateTime endDate)
        {
            var filter = Builders<TripLocationHistory>.Filter.And(
                Builders<TripLocationHistory>.Filter.Eq(x => x.TripId, tripId),
                Builders<TripLocationHistory>.Filter.Eq(x => x.IsDeleted, false),
                Builders<TripLocationHistory>.Filter.Gte(x => x.Location.RecordedAt, startDate),
                Builders<TripLocationHistory>.Filter.Lte(x => x.Location.RecordedAt, endDate)
            );

            var sort = Builders<TripLocationHistory>.Sort.Ascending(x => x.Location.RecordedAt);
            return await FindByFilterAsync(filter, sort);
        }
    }
}