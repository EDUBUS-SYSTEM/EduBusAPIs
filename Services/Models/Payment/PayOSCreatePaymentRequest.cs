namespace Services.Models.Payment;

public class PayOSCreatePaymentRequest
{
    public string OrderCode { get; set; } = string.Empty;
    public int Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
    public PayOSItem[] Items { get; set; } = Array.Empty<PayOSItem>();
}

public class PayOSItem
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int Price { get; set; }
}


