using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using Microsoft.EntityFrameworkCore;
using Services.Models.TransportFeeItem;
using Services.Contracts;

namespace Services.Implementations
{
    public class TransportFeeItemService : ITransportFeeItemService
    {
        private readonly ITransportFeeItemRepository _transportFeeItemRepo;
        private readonly IStudentRepository _studentRepo;
        private readonly ITransactionRepository _transactionRepo;
        private readonly IUnitPriceRepository _unitPriceRepo;
        private readonly DbContext _dbContext;

        public TransportFeeItemService(
            ITransportFeeItemRepository transportFeeItemRepo,
            IStudentRepository studentRepo,
            ITransactionRepository transactionRepo,
            IUnitPriceRepository unitPriceRepo,
            DbContext dbContext)
        {
            _transportFeeItemRepo = transportFeeItemRepo;
            _studentRepo = studentRepo;
            _transactionRepo = transactionRepo;
            _unitPriceRepo = unitPriceRepo;
            _dbContext = dbContext;
        }

        public async Task<TransportFeeItemDetailResponse> GetDetailAsync(Guid id)
        {
            var item = await _transportFeeItemRepo.FindAsync(id);
            if (item == null)
                throw new KeyNotFoundException("Transport fee item not found");

            var student = await _studentRepo.FindAsync(item.StudentId);
            var transaction = item.TransactionId.HasValue ? await _transactionRepo.FindAsync(item.TransactionId.Value) : null;

            return new TransportFeeItemDetailResponse
            {
                Id = item.Id,
                TransactionId = item.TransactionId ?? Guid.Empty,
                TransactionCode = transaction?.TransactionCode ?? "",
                StudentId = item.StudentId,
                StudentName = student != null ? $"{student.FirstName} {student.LastName}" : "",
                ParentEmail = item.ParentEmail,
                Description = item.Description,
                DistanceKm = item.DistanceKm,
                UnitPricePerKm = item.UnitPriceVndPerKm,
                Subtotal = item.Subtotal,
                SemesterName = item.SemesterName,
                AcademicYear = item.AcademicYear,
                SemesterStartDate = item.SemesterStartDate,
                SemesterEndDate = item.SemesterEndDate,
                TotalSchoolDays = 0, // Not stored in entity, calculated separately
                Status = item.Status,
                Type = item.Type,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt
            };
        }

        public async Task<TransportFeeItemListResponse> GetListAsync(TransportFeeItemListRequest request)
        {
            var (items, totalCount) = await _transportFeeItemRepo.GetListAsync(
                request.TransactionId,
                request.StudentId,
                request.ParentEmail,
                request.Status,
                request.SemesterName,
                request.AcademicYear,
                request.Type,
                request.Page,
                request.PageSize);

            return new TransportFeeItemListResponse
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
            };
        }

        public async Task<Data.Models.TransportFeeItem> CreateAsync(CreateTransportFeeItemRequest request)
        {
            // Business logic validation
            if (request.SemesterEndDate <= request.SemesterStartDate)
                throw new ArgumentException("Semester end date must be after semester start date");

            if (request.DistanceKm <= 0)
                throw new ArgumentException("Distance must be greater than 0");

            if (request.UnitPricePerKm <= 0)
                throw new ArgumentException("Unit price must be greater than 0");

            if (request.Subtotal <= 0)
                throw new ArgumentException("Subtotal must be greater than 0");

            // Validate student exists
            var student = await _studentRepo.FindAsync(request.StudentId);
            if (student == null)
                throw new ArgumentException("Student not found");

            // Validate transaction exists
            var transaction = await _transactionRepo.FindAsync(request.TransactionId);
            if (transaction == null)
                throw new ArgumentException("Transaction not found");

            // Get current active unit price if UnitPriceId is not provided
            Guid? unitPriceId = request.UnitPriceId;
            if (!unitPriceId.HasValue)
            {
                // Get current active unit price
                var activeUnitPrice = await _unitPriceRepo.GetQueryable()
                    .Where(up => up.IsActive && !up.IsDeleted)
                    .OrderByDescending(up => up.CreatedAt)
                    .FirstOrDefaultAsync();
                
                if (activeUnitPrice != null)
                {
                    unitPriceId = activeUnitPrice.Id;
                }
            }

            // Use subtotal from request (already calculated from calculate-fee API)
            var subtotal = request.Subtotal;

            var item = new Data.Models.TransportFeeItem
            {
                Id = Guid.NewGuid(),
                TransactionId = request.TransactionId,
                StudentId = request.StudentId,
                ParentEmail = request.ParentEmail,
                Description = request.Description,
                DistanceKm = request.DistanceKm,
                UnitPriceVndPerKm = request.UnitPricePerKm,
                Subtotal = subtotal,
                SemesterName = request.SemesterName,
                SemesterCode = request.SemesterName, // Use SemesterName as SemesterCode for now
                AcademicYear = request.AcademicYear,
                SemesterStartDate = request.SemesterStartDate,
                SemesterEndDate = request.SemesterEndDate,
                // TotalSchoolDays not stored in entity
                Status = TransportFeeItemStatus.Unbilled,
                Type = request.Type,
                UnitPriceId = unitPriceId,
                CreatedAt = DateTime.UtcNow
            };

            await _transportFeeItemRepo.AddAsync(item);
            await _dbContext.SaveChangesAsync();

            return item;
        }

        public async Task<bool> UpdateStatusAsync(UpdateTransportFeeItemStatusRequest request)
        {
            var item = await _transportFeeItemRepo.FindAsync(request.Id);
            if (item == null)
                return false;

            item.Status = request.Status;
            item.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(request.Notes))
            {
                item.Description += $" | Notes: {request.Notes}";
            }

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateStatusBatchAsync(List<Guid> ids, TransportFeeItemStatus status)
        {
            return await _transportFeeItemRepo.UpdateStatusBatchAsync(ids, status);
        }

        public async Task<List<TransportFeeItemSummary>> GetByTransactionIdAsync(Guid transactionId)
        {
            var items = await _transportFeeItemRepo.GetByTransactionIdAsync(transactionId);
            var studentIds = items.Select(i => i.StudentId).ToList();
            var students = await _studentRepo.GetQueryable()
                .Where(s => studentIds.Contains(s.Id))
                .ToListAsync();

            return items.Select(item =>
            {
                var student = students.FirstOrDefault(s => s.Id == item.StudentId);
                return new TransportFeeItemSummary
                {
                    Id = item.Id,
                    StudentId = item.StudentId,
                    StudentName = student != null ? $"{student.FirstName} {student.LastName}" : "",
                    Description = item.Description,
                    Amount = item.Subtotal,
                    Subtotal = item.Subtotal,
                    Status = item.Status,
                    CreatedAt = item.CreatedAt
                };
            }).ToList();
        }

        public async Task<List<TransportFeeItemSummary>> GetByStudentIdAsync(Guid studentId)
        {
            var items = await _transportFeeItemRepo.GetByStudentIdAsync(studentId);
            var student = await _studentRepo.FindAsync(studentId);

            return items.Select(item => new TransportFeeItemSummary
            {
                Id = item.Id,
                StudentName = student != null ? $"{student.FirstName} {student.LastName}" : "",
                Description = item.Description,
                Subtotal = item.Subtotal,
                Status = item.Status,
                CreatedAt = item.CreatedAt
            }).ToList();
        }

        public async Task<List<TransportFeeItemSummary>> GetByParentEmailAsync(string parentEmail)
        {
            var items = await _transportFeeItemRepo.GetByParentEmailAsync(parentEmail);
            var studentIds = items.Select(i => i.StudentId).ToList();
            var students = await _studentRepo.GetQueryable()
                .Where(s => studentIds.Contains(s.Id))
                .ToListAsync();

            return items.Select(item =>
            {
                var student = students.FirstOrDefault(s => s.Id == item.StudentId);
                return new TransportFeeItemSummary
                {
                    Id = item.Id,
                    StudentId = item.StudentId,
                    StudentName = student != null ? $"{student.FirstName} {student.LastName}" : "",
                    Description = item.Description,
                    Amount = item.Subtotal,
                    Subtotal = item.Subtotal,
                    Status = item.Status,
                    CreatedAt = item.CreatedAt
                };
            }).ToList();
        }

        public async Task<decimal> GetTotalAmountByTransactionIdAsync(Guid transactionId)
        {
            return await _transportFeeItemRepo.GetTotalAmountByTransactionIdAsync(transactionId);
        }

        public async Task<int> GetCountByStatusAsync(TransportFeeItemStatus status)
        {
            return await _transportFeeItemRepo.GetCountByStatusAsync(status);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var item = await _transportFeeItemRepo.FindAsync(id);
            if (item == null)
                return false;

            item.IsDeleted = true;
            item.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}
