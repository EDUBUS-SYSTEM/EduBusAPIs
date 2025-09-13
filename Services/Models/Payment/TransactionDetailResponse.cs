using Data.Models.Enums;

namespace Services.Models.Payment;

public class TransactionDetailResponse : TransactionSummaryResponse
{
    public string Description { get; set; } = string.Empty;
    public Guid ParentId { get; set; }
    public IEnumerable<TransportFeeItemResponse> Items { get; set; } = new List<TransportFeeItemResponse>();
    public PaymentProvider Provider { get; set; }
    public string? ProviderTransactionId { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public string? Metadata { get; set; }
}

