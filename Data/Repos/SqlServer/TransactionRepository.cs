using Data.Models;
using Data.Repos.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Data.Repos.SqlServer;

public class TransactionRepository : SqlRepository<Transaction>, ITransactionRepository
{
    public TransactionRepository(DbContext dbContext) : base(dbContext)
    {
    }

    public async Task<(List<Transaction> Items, int TotalCount)> GetTransactionsByStudentAsync(Guid studentId, int page, int pageSize)
    {
        var query = _dbContext.Set<Transaction>()
            .Where(t => !t.IsDeleted && 
                       _dbContext.Set<TransportFeeItem>()
                           .Any(tfi => tfi.TransactionId == t.Id && tfi.StudentId == studentId && !tfi.IsDeleted))
            .OrderByDescending(t => t.CreatedAt);

        var totalCount = await query.CountAsync();
        
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Transaction?> GetTransactionByTransportFeeItemIdAsync(Guid transportFeeItemId)
    {
        return await _dbContext.Set<Transaction>()
            .Where(t => !t.IsDeleted && 
                       _dbContext.Set<TransportFeeItem>()
                           .Any(tfi => tfi.Id == transportFeeItemId && tfi.TransactionId == t.Id && !tfi.IsDeleted))
            .FirstOrDefaultAsync();
    }
}


