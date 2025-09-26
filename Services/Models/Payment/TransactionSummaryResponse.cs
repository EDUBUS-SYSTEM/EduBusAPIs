using Data.Models.Enums;

namespace Services.Models.Payment;

public class TransactionSummaryResponse
{
    public Guid Id { get; set; }
    public string TransactionCode { get; set; } = string.Empty;
    public TransactionStatus Status { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public DateTime CreatedAtUtc { get; set; }
}

