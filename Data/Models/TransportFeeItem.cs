namespace Data.Models;

public partial class TransportFeeItem : BaseDomain
{
    public Guid StudentId { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; } = null!;

    public string? Content { get; set; }

    public Guid? TransactionId { get; set; }

    public virtual Student Student { get; set; } = null!;

    public virtual Transaction? Transaction { get; set; }
}
