using Microsoft.EntityFrameworkCore;
using Data.Contexts.SqlServer;
using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;

namespace Data.Repos.SqlServer;

public class TransactionRepository : SqlRepository<Transaction>, ITransactionRepository
{
    private readonly EduBusSqlContext _context;
    
    public TransactionRepository(EduBusSqlContext context) : base(context)
    {
        _context = context;
    }

    public async Task<Transaction?> GetByTransactionCodeAsync(string transactionCode)
    {
        return await _context.Transactions
            .FirstOrDefaultAsync(t => t.TransactionCode == transactionCode);
    }

    public async Task<Transaction?> GetByOrderCodeAsync(long orderCode)
    {
        return await _context.Transactions
            .FirstOrDefaultAsync(t => t.Metadata != null && t.Metadata.Contains($"\"orderCode\":{orderCode}"));
    }

    public async Task<List<Transaction>> GetByParentIdAsync(Guid parentId)
    {
        return await _context.Transactions
            .Where(t => t.ParentId == parentId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Transaction>> GetByStatusAsync(TransactionStatus status)
    {
        return await _context.Transactions
            .Where(t => t.Status == status)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }
}