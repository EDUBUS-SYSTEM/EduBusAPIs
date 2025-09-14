using System.ComponentModel.DataAnnotations;

namespace Services.Models.Payment;

/// <summary>
/// PayOS webhook payload model
/// </summary>
public class PayOSWebhookPayload
{
    /// <summary>
    /// Response code from PayOS
    /// </summary>
    [Required]
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// Response description from PayOS
    /// </summary>
    [Required]
    public string Desc { get; set; } = string.Empty;
    
    /// <summary>
    /// Success status
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Webhook data payload
    /// </summary>
    [Required]
    public PayOSWebhookData Data { get; set; } = new();
    
    /// <summary>
    /// HMAC SHA256 signature for verification
    /// </summary>
    [Required]
    public string Signature { get; set; } = string.Empty;
}

/// <summary>
/// PayOS webhook data model according to PayOS documentation
/// </summary>
public class PayOSWebhookData
{
    /// <summary>
    /// Order code (required)
    /// </summary>
    [Required]
    public long OrderCode { get; set; }
    
    /// <summary>
    /// Payment amount in VND (required)
    /// </summary>
    [Required]
    public int Amount { get; set; }
    
    /// <summary>
    /// Payment description (required)
    /// </summary>
    [Required]
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Account number (required)
    /// </summary>
    [Required]
    public string AccountNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// Transaction reference (required)
    /// </summary>
    [Required]
    public string Reference { get; set; } = string.Empty;
    
    /// <summary>
    /// Transaction date time (required)
    /// </summary>
    [Required]
    public string TransactionDateTime { get; set; } = string.Empty;
    
    /// <summary>
    /// Currency code (required)
    /// </summary>
    [Required]
    public string Currency { get; set; } = string.Empty;
    
    /// <summary>
    /// Payment link ID (required)
    /// </summary>
    [Required]
    public string PaymentLinkId { get; set; } = string.Empty;
    
    /// <summary>
    /// Transaction code (required)
    /// </summary>
    [Required]
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// Transaction description (required)
    /// </summary>
    [Required]
    public string Desc { get; set; } = string.Empty;
    
    /// <summary>
    /// Counter account bank ID (optional)
    /// </summary>
    public string? CounterAccountBankId { get; set; }
    
    /// <summary>
    /// Counter account bank name (optional)
    /// </summary>
    public string? CounterAccountBankName { get; set; }
    
    /// <summary>
    /// Counter account name (optional)
    /// </summary>
    public string? CounterAccountName { get; set; }
    
    /// <summary>
    /// Counter account number (optional)
    /// </summary>
    public string? CounterAccountNumber { get; set; }
    
    /// <summary>
    /// Virtual account name (optional)
    /// </summary>
    public string? VirtualAccountName { get; set; }
    
    /// <summary>
    /// Virtual account number (optional)
    /// </summary>
    public string? VirtualAccountNumber { get; set; }
}

