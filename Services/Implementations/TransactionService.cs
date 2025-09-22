using Microsoft.EntityFrameworkCore;
using Services.Contracts;
using Services.Models.Transaction;
using Data.Contexts.SqlServer;
using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Services.Implementations;

public class TransactionService : ITransactionService
{
    private readonly EduBusSqlContext _context;
    private readonly ITransactionRepository _transactionRepo;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(
        EduBusSqlContext context,
        ITransactionRepository transactionRepo,
        IPaymentService paymentService,
        ILogger<TransactionService> logger)
    {
        _context = context;
        _transactionRepo = transactionRepo;
        _paymentService = paymentService;
        _logger = logger;
    }

    public async Task<PagedTransactionDto> GetTransactionsAsync(TransactionListRequest request)
    {
        var query = _context.Transactions
            .Include(t => t.Parent)
            .AsQueryable();

        if (request.Status.HasValue)
            query = query.Where(t => t.Status == request.Status.Value);

        if (request.ParentId.HasValue)
            query = query.Where(t => t.ParentId == request.ParentId.Value);

        if (request.From.HasValue)
            query = query.Where(t => t.CreatedAt >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(t => t.CreatedAt <= request.To.Value);

        var totalCount = await query.CountAsync();

        var sortQuery = request.SortBy.ToLower() switch
        {
            "amount" => request.SortOrder == "asc" 
                ? query.OrderBy(t => t.Amount) 
                : query.OrderByDescending(t => t.Amount),
            "status" => request.SortOrder == "asc" 
                ? query.OrderBy(t => t.Status) 
                : query.OrderByDescending(t => t.Status),
            "paiddate" => request.SortOrder == "asc" 
                ? query.OrderBy(t => t.PaidAtUtc) 
                : query.OrderByDescending(t => t.PaidAtUtc),
            _ => request.SortOrder == "asc" 
                ? query.OrderBy(t => t.CreatedAt) 
                : query.OrderByDescending(t => t.CreatedAt)
        };

        var items = await sortQuery
            .Skip((request.Page - 1) * request.PerPage)
            .Take(request.PerPage)
            .Select(t => new TransactionSummaryDto
            {
                Id = t.Id,
                ParentId = t.ParentId,
                ParentName = t.Parent.FirstName + " " + t.Parent.LastName,
                TransactionCode = t.TransactionCode,
                Status = t.Status,
                Amount = t.Amount,
                Currency = t.Currency,
                Description = t.Description,
                Provider = t.Provider,
                PaidAtUtc = t.PaidAtUtc,
                CreatedAtUtc = t.CreatedAt
            })
            .ToListAsync();

        return new PagedTransactionDto
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PerPage = request.PerPage
        };
    }

    public async Task<TransactionDetailDto?> GetTransactionDetailAsync(Guid id)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Parent)
            .Include(t => t.TransportFeeItems)
                .ThenInclude(tfi => tfi.Student)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (transaction == null)
            return null;

        return new TransactionDetailDto
        {
            Id = transaction.Id,
            ParentId = transaction.ParentId,
            ParentName = transaction.Parent.FirstName + " " + transaction.Parent.LastName,
            TransactionCode = transaction.TransactionCode,
            Status = transaction.Status,
            Amount = transaction.Amount,
            Currency = transaction.Currency,
            Description = transaction.Description,
            Provider = transaction.Provider,
            ProviderTransactionId = transaction.ProviderTransactionId,
            QrCodeUrl = transaction.QrCodeUrl,
            QrContent = transaction.QrContent,
            QrExpiredAtUtc = transaction.QrExpiredAtUtc,
            PaidAtUtc = transaction.PaidAtUtc,
            PickupPointRequestId = transaction.PickupPointRequestId,
            ScheduleId = transaction.ScheduleId,
            Metadata = transaction.Metadata,
            TransportFeeItems = transaction.TransportFeeItems.Select(tfi => new TransportFeeItemDto
            {
                Id = tfi.Id,
                StudentId = tfi.StudentId,
                StudentName = tfi.Student.FirstName + " " + tfi.Student.LastName,
                Description = tfi.Description,
                DistanceKm = tfi.DistanceKm,
                UnitPriceVndPerKm = tfi.UnitPriceVndPerKm,
                QuantityKm = tfi.QuantityKm,
                Subtotal = tfi.Subtotal,
                PeriodMonth = tfi.PeriodMonth,
                PeriodYear = tfi.PeriodYear,
                Status = tfi.Status
            }).ToList(),
            CreatedAtUtc = transaction.CreatedAt,
            UpdatedAtUtc = transaction.UpdatedAt ?? transaction.CreatedAt
        };
    }

    public async Task<TransactionDetailDto> CreateTransactionAsync(CreateTransactionRequestDto request)
    {
        var transaction = new Transaction
        {
            ParentId = request.ParentId,
            TransactionCode = GenerateTransactionCode(),
            Status = TransactionStatus.Pending,
            Amount = request.Amount,
            Currency = "VND",
            Description = request.Description,
            Provider = PaymentProvider.PayOS,
            PickupPointRequestId = request.PickupPointRequestId,
            ScheduleId = request.ScheduleId,
            Metadata = request.Metadata
        };

        await _transactionRepo.AddAsync(transaction);

        // Create transport fee items
        foreach (var item in request.TransportFeeItems)
        {
            var transportFeeItem = new TransportFeeItem
            {
                StudentId = item.StudentId,
                Description = item.Description,
                DistanceKm = item.DistanceKm,
                UnitPriceVndPerKm = item.UnitPriceVndPerKm,
                QuantityKm = item.QuantityKm,
                Subtotal = item.Subtotal,
                PeriodMonth = item.PeriodMonth,
                PeriodYear = item.PeriodYear,
                Status = TransportFeeItemStatus.Pending,
                TransactionId = transaction.Id
            };

            _context.TransportFeeItems.Add(transportFeeItem);
        }

        await _context.SaveChangesAsync();

        return await GetTransactionDetailAsync(transaction.Id) ?? throw new InvalidOperationException("Failed to retrieve created transaction");
    }

    public async Task<TransactionDetailDto?> ApproveTransactionAsync(Guid id)
    {
        var transaction = await _transactionRepo.FindAsync(id);
        if (transaction == null)
            return null;

        if (transaction.Status != TransactionStatus.Pending)
            throw new InvalidOperationException("Only pending transactions can be approved");

        transaction.Status = TransactionStatus.Approved;
        await _transactionRepo.UpdateAsync(transaction);

        return await GetTransactionDetailAsync(id);
    }

    public async Task<TransactionDetailDto?> RejectTransactionAsync(Guid id, RejectTransactionRequestDto request)
    {
        var transaction = await _transactionRepo.FindAsync(id);
        if (transaction == null)
            return null;

        if (transaction.Status != TransactionStatus.Pending)
            throw new InvalidOperationException("Only pending transactions can be rejected");

        transaction.Status = TransactionStatus.Rejected;
        transaction.Metadata = JsonSerializer.Serialize(new { 
            RejectionReason = request.Reason, 
            AdminNotes = request.AdminNotes,
            RejectedAt = DateTime.UtcNow
        });
        
        await _transactionRepo.UpdateAsync(transaction);

        return await GetTransactionDetailAsync(id);
    }

    public async Task<List<TransactionHistoryDto>> GetTransactionHistoryAsync(Guid id)
    {
        var events = await _context.PaymentEventLogs
            .Where(e => e.TransactionId == id)
            .OrderBy(e => e.AtUtc)
            .Select(e => new TransactionHistoryDto
            {
                Id = e.Id,
                TransactionId = e.TransactionId,
                Status = e.Status,
                Description = e.Message ?? "",
                Source = e.Source,
                AtUtc = e.AtUtc,
                Metadata = e.RawPayload
            })
            .ToListAsync();

        return events;
    }

    public async Task<List<TransactionSummaryDto>> GetParentTransactionsAsync(Guid parentId)
    {
        var transactions = await _context.Transactions
            .Include(t => t.Parent)
            .Where(t => t.ParentId == parentId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TransactionSummaryDto
            {
                Id = t.Id,
                ParentId = t.ParentId,
                ParentName = t.Parent.FirstName + " " + t.Parent.LastName,
                TransactionCode = t.TransactionCode,
                Status = t.Status,
                Amount = t.Amount,
                Currency = t.Currency,
                Description = t.Description,
                Provider = t.Provider,
                PaidAtUtc = t.PaidAtUtc,
                CreatedAtUtc = t.CreatedAt
            })
            .ToListAsync();

        return transactions;
    }

    public async Task<TransactionDetailDto?> CreateTransactionFromPickupApprovalAsync(Guid pickupPointRequestId, Guid adminId)
    {
        // This method will be implemented when integrating with pickup point approval
        // For now, return null as placeholder
        return null;
    }

    private string GenerateTransactionCode()
    {
        return "TXN" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + Guid.NewGuid().ToString("N")[..8].ToUpper();
    }
}
