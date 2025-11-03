using Data.Models;
using Services.Models.Payment;

namespace Services.Contracts;

public interface IPaymentService
{
    // Transaction Management
    Task<PagedTransactionResponse> GetTransactionsAsync(TransactionListRequest request);
    Task<TransactionDetailResponse?> GetTransactionDetailAsync(Guid transactionId);
    Task<Transaction?> GetTransactionByOrderCodeAsync(long orderCode);
    Task<QrResponse> GenerateOrRefreshQrAsync(Guid transactionId);
    Task<IEnumerable<PaymentEventResponse>> GetTransactionEventsAsync(Guid transactionId);
    
    // Admin Operations
    Task<TransactionSummaryResponse> CancelTransactionAsync(Guid transactionId);
    Task<TransactionSummaryResponse> MarkTransactionAsPaidAsync(Guid transactionId, MarkPaidRequest request);
    
    // Webhook Handling
    Task<bool> HandlePayOSWebhookAsync(PayOSWebhookPayload payload);
    
    // Business Logic
    Task<Transaction> CreateTransactionForPickupPointRequestAsync(string pickupPointRequestId, Guid scheduleId);
    Task<bool> IsTransactionCancellableAsync(Transaction transaction);
    Task<bool> IsTransactionPaidAsync(Transaction transaction);
    Task<UnpaidFeesResponse> GetUnpaidFeesAsync(Guid parentId);
}

