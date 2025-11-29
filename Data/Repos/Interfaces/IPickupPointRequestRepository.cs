using System;
using System.Collections.Generic;
using Data.Models;

namespace Data.Repos.Interfaces
{
    public interface IPickupPointRequestRepository : IMongoRepository<PickupPointRequestDocument>
    {
        Task<List<PickupPointRequestDocument>> QueryAsync(string? status, string? parentEmail, int skip, int take);
        Task<List<PickupPointRequestDocument>> GetActiveRequestsByStudentIdsAsync(
            IEnumerable<Guid> studentIds,
            string semesterCode,
            params string[] statuses);
    }
}
