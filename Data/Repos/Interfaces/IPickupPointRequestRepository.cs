using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repos.Interfaces
{
    public interface IPickupPointRequestRepository : IMongoRepository<PickupPointRequestDocument>
    {
        Task<PickupPointRequestDocument> AddAsync(PickupPointRequestDocument doc); 
        Task<List<PickupPointRequestDocument>> QueryAsync(string? status, string? parentEmail, int skip, int take);
    }
}
