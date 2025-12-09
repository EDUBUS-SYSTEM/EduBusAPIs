using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Data.Repos.SqlServer
{
    public class TransportFeeItemRepository : SqlRepository<TransportFeeItem>, ITransportFeeItemRepository
    {
        public TransportFeeItemRepository(DbContext context) : base(context)
        {
        }

        public async Task<List<TransportFeeItem>> GetByTransactionIdAsync(Guid transactionId)
        {
            return await GetQueryable()
                .Where(tfi => tfi.TransactionId == transactionId && !tfi.IsDeleted)
                .OrderBy(tfi => tfi.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<TransportFeeItem>> GetByStudentIdAsync(Guid studentId)
        {
            return await GetQueryable()
                .Where(tfi => tfi.StudentId == studentId && !tfi.IsDeleted)
                .OrderByDescending(tfi => tfi.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<TransportFeeItem>> GetByParentEmailAsync(string parentEmail)
        {
            return await GetQueryable()
                .Where(tfi => tfi.ParentEmail == parentEmail && !tfi.IsDeleted)
                .OrderByDescending(tfi => tfi.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<TransportFeeItem>> GetByParentAndSemesterCodeAsync(string parentEmail, string semesterCode)
        {
            return await GetQueryable()
                .Where(tfi => tfi.ParentEmail == parentEmail && tfi.SemesterCode == semesterCode && !tfi.IsDeleted)
                .ToListAsync();
        }

        public async Task<List<TransportFeeItem>> GetByStatusAsync(TransportFeeItemStatus status)
        {
            return await GetQueryable()
                .Where(tfi => tfi.Status == status && !tfi.IsDeleted)
                .OrderByDescending(tfi => tfi.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<TransportFeeItem>> GetBySemesterAsync(string semesterName, string academicYear)
        {
            return await GetQueryable()
                .Where(tfi => tfi.SemesterName == semesterName && 
                             tfi.AcademicYear == academicYear && 
                             !tfi.IsDeleted)
                .OrderByDescending(tfi => tfi.CreatedAt)
                .ToListAsync();
        }

        public async Task<(List<TransportFeeItem> Items, int TotalCount)> GetListAsync(Guid? transactionId, Guid? studentId, string? parentEmail, 
            TransportFeeItemStatus? status, string? semesterName, string? academicYear, 
            TransportFeeItemType? type, int page, int pageSize)
        {
            var query = GetQueryable().Where(tfi => !tfi.IsDeleted);

            // Apply filters
            if (transactionId.HasValue)
                query = query.Where(tfi => tfi.TransactionId == transactionId.Value);

            if (studentId.HasValue)
                query = query.Where(tfi => tfi.StudentId == studentId.Value);

            if (!string.IsNullOrWhiteSpace(parentEmail))
                query = query.Where(tfi => tfi.ParentEmail.Contains(parentEmail));

            if (status.HasValue)
                query = query.Where(tfi => tfi.Status == status.Value);

            if (!string.IsNullOrWhiteSpace(semesterName))
                query = query.Where(tfi => tfi.SemesterName.Contains(semesterName));

            if (!string.IsNullOrWhiteSpace(academicYear))
                query = query.Where(tfi => tfi.AcademicYear.Contains(academicYear));

            if (type.HasValue)
                query = query.Where(tfi => tfi.Type == type.Value);

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var items = await query
                .OrderByDescending(tfi => tfi.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<decimal> GetTotalAmountByTransactionIdAsync(Guid transactionId)
        {
            return await GetQueryable()
                .Where(tfi => tfi.TransactionId == transactionId && !tfi.IsDeleted)
                .SumAsync(tfi => tfi.Subtotal);
        }

        public async Task<int> GetCountByStatusAsync(TransportFeeItemStatus status)
        {
            return await GetQueryable()
                .Where(tfi => tfi.Status == status && !tfi.IsDeleted)
                .CountAsync();
        }

        public async Task<bool> UpdateStatusAsync(Guid id, TransportFeeItemStatus status)
        {
            var item = await GetQueryable()
                .FirstOrDefaultAsync(tfi => tfi.Id == id && !tfi.IsDeleted);

            if (item == null) return false;

            item.Status = status;
            item.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateStatusBatchAsync(List<Guid> ids, TransportFeeItemStatus status)
        {
            var items = await GetQueryable()
                .Where(tfi => ids.Contains(tfi.Id) && !tfi.IsDeleted)
                .ToListAsync();

            if (!items.Any()) return false;

            foreach (var item in items)
            {
                item.Status = status;
                item.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}