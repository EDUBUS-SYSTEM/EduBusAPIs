using Data.Models.Enums;

namespace Services.Models.Payment;

public class PaymentEventResponse
{
    public Guid Id { get; set; }
    public Guid TransactionId { get; set; }
    public TransactionStatus Status { get; set; }
    public DateTime AtUtc { get; set; }
    public PaymentEventSource Source { get; set; }
    public string Message { get; set; } = string.Empty;
}

