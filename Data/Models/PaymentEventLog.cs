namespace Data.Models;
using Data.Models.Enums;

public class PaymentEventLog : BaseDomain
{
    public Guid TransactionId { get; set; }
    public TransactionStatus Status { get; set; }
    public DateTime AtUtc { get; set; } = DateTime.UtcNow;
    public PaymentEventSource Source { get; set; }
    public string? Message { get; set; }
    public string? RawPayload { get; set; }
}


