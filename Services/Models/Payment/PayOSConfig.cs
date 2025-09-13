namespace Services.Models.Payment;

public class PayOSConfig
{
    public string ClientId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ChecksumKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api-merchant.payos.vn";
    public string WebhookUrl { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = "https://edubus.app/payment/success";
    public string CancelUrl { get; set; } = "https://edubus.app/payment/cancel";
    public int QrExpirationMinutes { get; set; } = 15;
}

