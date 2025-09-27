using Data.Models;

namespace Data.Repos.Interfaces;

public interface ITransactionRepository : ISqlRepository<Transaction>
{
    Task<(List<Transaction> Items, int TotalCount)> GetTransactionsByStudentAsync(Guid studentId, int page, int pageSize);
    Task<Transaction?> GetTransactionByTransportFeeItemIdAsync(Guid transportFeeItemId);
}

