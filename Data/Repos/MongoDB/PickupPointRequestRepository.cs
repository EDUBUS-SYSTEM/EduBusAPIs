using Data.Models;
using Data.Repos.Interfaces;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;

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

        public async Task<List<PickupPointRequestDocument>> GetActiveRequestsByStudentIdsAsync(
            IEnumerable<Guid> studentIds,
            string semesterCode,
            params string[] statuses)
        {
            var idList = studentIds?
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList() ?? new List<Guid>();

            if (!idList.Any() || string.IsNullOrWhiteSpace(semesterCode))
            {
                return new List<PickupPointRequestDocument>();
            }

            var filter = Builders<PickupPointRequestDocument>.Filter.And(
                Builders<PickupPointRequestDocument>.Filter.Eq(x => x.IsDeleted, false),
                Builders<PickupPointRequestDocument>.Filter.Eq(x => x.SemesterCode, semesterCode),
                Builders<PickupPointRequestDocument>.Filter.AnyIn(x => x.StudentIds, idList));

            var statusList = (statuses?.Length ?? 0) > 0
                ? statuses
                : new[] { "Pending", "Approved" };

            filter &= Builders<PickupPointRequestDocument>.Filter.In(x => x.Status, statusList);

            return await _collection.Find(filter).ToListAsync();
        }
    }
}
