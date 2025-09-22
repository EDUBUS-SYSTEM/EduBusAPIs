using Data.Models;
using Data.Repos.Interfaces;

namespace Data.Repos.Interfaces;

public interface ITransactionRepository : ISqlRepository<Transaction>
{
    Task<Transaction?> GetByTransactionCodeAsync(string transactionCode);
    Task<Transaction?> GetByOrderCodeAsync(long orderCode);
    Task<List<Transaction>> GetByParentIdAsync(Guid parentId);
    Task<List<Transaction>> GetByStatusAsync(Data.Models.Enums.TransactionStatus status);
}