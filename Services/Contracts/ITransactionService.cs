using Services.Models.Transaction;

namespace Services.Contracts;

public interface ITransactionService
{
    Task<PagedTransactionDto> GetTransactionsAsync(TransactionListRequest request);
    Task<TransactionDetailDto?> GetTransactionDetailAsync(Guid id);
    Task<TransactionDetailDto> CreateTransactionAsync(CreateTransactionRequestDto request);
    Task<TransactionDetailDto?> ApproveTransactionAsync(Guid id);
    Task<TransactionDetailDto?> RejectTransactionAsync(Guid id, RejectTransactionRequestDto request);
    Task<List<TransactionHistoryDto>> GetTransactionHistoryAsync(Guid id);
    Task<List<TransactionSummaryDto>> GetParentTransactionsAsync(Guid parentId);
    Task<TransactionDetailDto?> CreateTransactionFromPickupApprovalAsync(Guid pickupPointRequestId, Guid adminId);
}
