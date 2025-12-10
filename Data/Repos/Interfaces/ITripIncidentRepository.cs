using System;
using System.Collections.Generic;
using Data.Models;
using Data.Models.Enums;

namespace Data.Repos.Interfaces
{
    public interface ITripIncidentRepository : IMongoRepository<TripIncidentReport>
    {
        Task<(IEnumerable<TripIncidentReport> Items, long TotalCount)> GetByTripAsync(
            Guid tripId,
            int page,
            int perPage);

        Task<(IEnumerable<TripIncidentReport> Items, long TotalCount)> GetAllAsync(
            Guid? tripId,
            Guid? supervisorId,
            TripIncidentStatus? status,
            int page,
            int perPage);
    }
}

