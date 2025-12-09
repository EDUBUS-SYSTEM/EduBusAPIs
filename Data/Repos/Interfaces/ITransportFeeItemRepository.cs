using Data.Models;
using Data.Models.Enums;

namespace Data.Repos.Interfaces
{
    public interface ITransportFeeItemRepository : ISqlRepository<TransportFeeItem>
    {
        Task<List<TransportFeeItem>> GetByTransactionIdAsync(Guid transactionId);
        Task<List<TransportFeeItem>> GetByStudentIdAsync(Guid studentId);
        Task<List<TransportFeeItem>> GetByParentEmailAsync(string parentEmail);
        Task<List<TransportFeeItem>> GetByParentAndSemesterCodeAsync(string parentEmail, string semesterCode);
        Task<List<TransportFeeItem>> GetByStatusAsync(TransportFeeItemStatus status);
        Task<List<TransportFeeItem>> GetBySemesterAsync(string semesterName, string academicYear);
        Task<(List<TransportFeeItem> Items, int TotalCount)> GetListAsync(Guid? transactionId, Guid? studentId, string? parentEmail, 
            TransportFeeItemStatus? status, string? semesterName, string? academicYear, 
            TransportFeeItemType? type, int page, int pageSize);
        Task<decimal> GetTotalAmountByTransactionIdAsync(Guid transactionId);
        Task<int> GetCountByStatusAsync(TransportFeeItemStatus status);
        Task<bool> UpdateStatusAsync(Guid id, TransportFeeItemStatus status);
        Task<bool> UpdateStatusBatchAsync(List<Guid> ids, TransportFeeItemStatus status);
    }
}