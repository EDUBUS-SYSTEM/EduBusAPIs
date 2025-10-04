using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using Microsoft.EntityFrameworkCore;
using Services.Models.TransportFeeItem;

namespace Services.Contracts
{
    public interface ITransportFeeItemService
    {
        Task<TransportFeeItemDetailResponse> GetDetailAsync(Guid id);
        Task<TransportFeeItemListResponse> GetListAsync(TransportFeeItemListRequest request);
        Task<Data.Models.TransportFeeItem> CreateAsync(CreateTransportFeeItemRequest request);
        Task<bool> UpdateStatusAsync(UpdateTransportFeeItemStatusRequest request);
        Task<bool> UpdateStatusBatchAsync(List<Guid> ids, TransportFeeItemStatus status);
        Task<List<TransportFeeItemSummary>> GetByTransactionIdAsync(Guid transactionId);
        Task<List<TransportFeeItemSummary>> GetByStudentIdAsync(Guid studentId);
        Task<List<TransportFeeItemSummary>> GetByParentEmailAsync(string parentEmail);
        Task<decimal> GetTotalAmountByTransactionIdAsync(Guid transactionId);
        Task<int> GetCountByStatusAsync(TransportFeeItemStatus status);
        Task<bool> DeleteAsync(Guid id);
    }
}
