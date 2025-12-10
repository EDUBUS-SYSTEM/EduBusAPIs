namespace Data.Models;
using Data.Models.Enums;

public partial class Transaction : BaseDomain
{
    public Guid ParentId { get; set; }
    public string TransactionCode { get; set; } = null!;
    public TransactionStatus Status { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public string Description { get; set; } = null!;
    public PaymentProvider Provider { get; set; }
    public string? ProviderTransactionId { get; set; }
    public string? QrCodeUrl { get; set; }
    public string? QrContent { get; set; }
    public DateTime? QrExpiredAtUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public string? PickupPointRequestId { get; set; }

    public virtual Parent Parent { get; set; } = null!;
    public virtual ICollection<TransportFeeItem> TransportFeeItems { get; set; } = new List<TransportFeeItem>();

	public Guid? RelocationRequestId { get; set; }
	public string TransactionType { get; set; } = TransactionTypeConstants.InitialPayment;

	public static class TransactionTypeConstants
	{
		public const string InitialPayment = "InitialPayment";
		public const string AdditionalPayment = "AdditionalPayment";
		public const string Refund = "Refund";
		public const string Adjustment = "Adjustment";
	}
}
