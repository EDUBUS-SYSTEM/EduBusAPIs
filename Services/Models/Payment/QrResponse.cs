namespace Services.Models.Payment;

public class QrResponse
{
    public string QrCode { get; set; } = string.Empty;
    public string CheckoutUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

