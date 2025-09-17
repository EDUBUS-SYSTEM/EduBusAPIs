using Data.Models;
using Data.Repos.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Data.Repos.SqlServer;

public class TransactionRepository : SqlRepository<Transaction>, ITransactionRepository
{
    public TransactionRepository(DbContext dbContext) : base(dbContext)
    {
    }
}


