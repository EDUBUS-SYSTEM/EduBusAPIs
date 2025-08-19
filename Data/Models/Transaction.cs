namespace Data.Models;

public partial class Transaction : BaseDomain
{
    public Guid ParentId { get; set; }

    public string TransactionCode { get; set; } = null!;

    public decimal Amount { get; set; }

    public virtual Parent Parent { get; set; } = null!;

    public virtual ICollection<TransportFeeItem> TransportFeeItems { get; set; } = new List<TransportFeeItem>();
}
