namespace Services.Models.Payment;

public class MarkPaidRequest
{
    public string? ProviderTransactionId { get; set; }
    public string? Note { get; set; }
}

