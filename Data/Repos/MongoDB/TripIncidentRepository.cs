using System;
using System.Collections.Generic;
using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using MongoDB.Driver;

namespace Data.Repos.MongoDB
{
    public class TripIncidentRepository : MongoRepository<TripIncidentReport>, ITripIncidentRepository
    {
        public TripIncidentRepository(IMongoDatabase database)
            : base(database, "trip_incidents")
        {
        }

        public async Task<(IEnumerable<TripIncidentReport> Items, long TotalCount)> GetByTripAsync(
            Guid tripId,
            int page,
            int perPage)
        {
            var filter = Builders<TripIncidentReport>.Filter.And(
                Builders<TripIncidentReport>.Filter.Eq(x => x.TripId, tripId),
                Builders<TripIncidentReport>.Filter.Eq(x => x.IsDeleted, false));

            var sort = Builders<TripIncidentReport>.Sort.Descending(x => x.CreatedAt);
            var skip = Math.Max(0, (page - 1) * perPage);

            var totalCount = await _collection.CountDocumentsAsync(filter);
            var items = await _collection
                .Find(filter)
                .Sort(sort)
                .Skip(skip)
                .Limit(perPage)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(IEnumerable<TripIncidentReport> Items, long TotalCount)> GetAllAsync(
            Guid? tripId,
            Guid? supervisorId,
            TripIncidentStatus? status,
            int page,
            int perPage)
        {
            var filters = new List<FilterDefinition<TripIncidentReport>>
            {
                Builders<TripIncidentReport>.Filter.Eq(x => x.IsDeleted, false)
            };

            if (tripId.HasValue)
            {
                filters.Add(Builders<TripIncidentReport>.Filter.Eq(x => x.TripId, tripId.Value));
            }

            if (supervisorId.HasValue)
            {
                filters.Add(Builders<TripIncidentReport>.Filter.Eq(x => x.SupervisorId, supervisorId.Value));
            }

            if (status.HasValue)
            {
                filters.Add(Builders<TripIncidentReport>.Filter.Eq(x => x.Status, status.Value));
            }

            var filter = Builders<TripIncidentReport>.Filter.And(filters);
            var sort = Builders<TripIncidentReport>.Sort.Descending(x => x.CreatedAt);
            var skip = Math.Max(0, (page - 1) * perPage);

            var totalCount = await _collection.CountDocumentsAsync(filter);
            var items = await _collection
                .Find(filter)
                .Sort(sort)
                .Skip(skip)
                .Limit(perPage)
                .ToListAsync();

            return (items, totalCount);
        }
    }
}

