using Data.Models.Enums;

namespace Services.Models.Transaction;

public class TransactionListRequest
{
    public TransactionStatus? Status { get; set; }
    public Guid? ParentId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int PerPage { get; set; } = 20;
    public string SortBy { get; set; } = "createdAtUtc";
    public string SortOrder { get; set; } = "desc";
}

public class PagedTransactionDto
{
    public List<TransactionSummaryDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PerPage { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PerPage);
}

public class TransactionSummaryDto
{
    public Guid Id { get; set; }
    public Guid ParentId { get; set; }
    public string ParentName { get; set; } = string.Empty;
    public string TransactionCode { get; set; } = string.Empty;
    public TransactionStatus Status { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PaymentProvider Provider { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class TransactionDetailDto
{
    public Guid Id { get; set; }
    public Guid ParentId { get; set; }
    public string ParentName { get; set; } = string.Empty;
    public string TransactionCode { get; set; } = string.Empty;
    public TransactionStatus Status { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PaymentProvider Provider { get; set; }
    public string? ProviderTransactionId { get; set; }
    public string? QrCodeUrl { get; set; }
    public string? QrContent { get; set; }
    public DateTime? QrExpiredAtUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public string? PickupPointRequestId { get; set; }
    public Guid? ScheduleId { get; set; }
    public string? Metadata { get; set; }
    public List<TransportFeeItemDto> TransportFeeItems { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class TransportFeeItemDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double DistanceKm { get; set; }
    public decimal UnitPriceVndPerKm { get; set; }
    public double QuantityKm { get; set; }
    public decimal Subtotal { get; set; }
    public int PeriodMonth { get; set; }
    public int PeriodYear { get; set; }
    public TransportFeeItemStatus Status { get; set; }
}

public class CreateTransactionRequestDto
{
    public Guid ParentId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? PickupPointRequestId { get; set; }
    public Guid? ScheduleId { get; set; }
    public string? Metadata { get; set; }
    public List<CreateTransportFeeItemRequestDto> TransportFeeItems { get; set; } = new();
}

public class CreateTransportFeeItemRequestDto
{
    public Guid StudentId { get; set; }
    public string Description { get; set; } = string.Empty;
    public double DistanceKm { get; set; }
    public decimal UnitPriceVndPerKm { get; set; }
    public double QuantityKm { get; set; }
    public decimal Subtotal { get; set; }
    public int PeriodMonth { get; set; }
    public int PeriodYear { get; set; }
}

public class RejectTransactionRequestDto
{
    public string Reason { get; set; } = string.Empty;
    public string? AdminNotes { get; set; }
}

public class TransactionHistoryDto
{
    public Guid Id { get; set; }
    public Guid TransactionId { get; set; }
    public TransactionStatus Status { get; set; }
    public string Description { get; set; } = string.Empty;
    public PaymentEventSource Source { get; set; }
    public DateTime AtUtc { get; set; }
    public string? Metadata { get; set; }
}
