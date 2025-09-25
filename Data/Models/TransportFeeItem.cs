namespace Data.Models;
using Data.Models.Enums;

public partial class TransportFeeItem : BaseDomain
{
    public Guid StudentId { get; set; }

    public string Description { get; set; } = null!;
    public double DistanceKm { get; set; }
    public decimal UnitPriceVndPerKm { get; set; }
    public double QuantityKm { get; set; }
    public decimal Subtotal { get; set; }
    public int PeriodMonth { get; set; }
    public int PeriodYear { get; set; }

    public TransportFeeItemStatus Status { get; set; }
    public Guid? TransactionId { get; set; }
    public Guid? UnitPriceId { get; set; }

    public virtual Student Student { get; set; } = null!;
    public virtual Transaction? Transaction { get; set; }
    public virtual UnitPrice? UnitPrice { get; set; }
}
