using Services.Models.Transaction;
using Data.Models.Enums;

namespace Services.Contracts
{
    public interface ITransactionService
    {
        Task<CreateTransactionFromPickupPointResponse> CreateTransactionFromPickupPointAsync(
            CreateTransactionFromPickupPointRequest request);

        Task<TransactionDetailResponseDto> GetTransactionDetailAsync(Guid transactionId);
        Task<TransactionListResponseDto> GetTransactionListAsync(TransactionListRequest request);
        Task<TransactionListResponseDto> GetTransactionsByStudentAsync(Guid studentId, int page, int pageSize);
        Task<TransactionDetailResponseDto> GetTransactionByTransportFeeItemIdAsync(Guid transportFeeItemId);
        Task<bool> UpdateTransactionStatusAsync(Guid transactionId, TransactionStatus status);
        Task<bool> DeleteTransactionAsync(Guid transactionId);
        Task<bool> UpdateTransactionAsync(Guid transactionId, dynamic request);
        
        // Business logic methods
        Task<CalculateFeeResponse> CalculateTransportFeeAsync(CalculateFeeRequest request);
        Task<AcademicSemesterInfo> GetNextSemesterAsync();
        Task<Data.Models.UnitPrice> GetCurrentActiveUnitPriceAsync(Guid? unitPriceId);
    }
}
