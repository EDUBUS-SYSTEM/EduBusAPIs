using Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repos.Interfaces
{
    public interface ITripLocationHistoryRepository : IMongoRepository<TripLocationHistory>
    {
        Task<IEnumerable<TripLocationHistory>> GetLocationHistoryByTripIdAsync(Guid tripId);
        Task<TripLocationHistory?> GetLatestLocationByTripIdAsync(Guid tripId);
        Task<IEnumerable<TripLocationHistory>> GetLocationHistoryByTripIdAndDateRangeAsync(
            Guid tripId,
            DateTime startDate,
            DateTime endDate);
    }
}
