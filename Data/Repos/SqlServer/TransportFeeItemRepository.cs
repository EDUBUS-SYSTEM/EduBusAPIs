using Data.Models;
using Data.Repos.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Data.Repos.SqlServer;

public class TransportFeeItemRepository : SqlRepository<TransportFeeItem>, ITransportFeeItemRepository
{
    public TransportFeeItemRepository(DbContext dbContext) : base(dbContext)
    {
    }
}


