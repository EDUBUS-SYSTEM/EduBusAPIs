namespace Data.Models;
using Data.Models.Enums;
using System.Text.Json.Serialization;

public partial class TransportFeeItem : BaseDomain
{
    public Guid StudentId { get; set; }

    public string Description { get; set; } = null!;
    public double DistanceKm { get; set; }
    public decimal UnitPriceVndPerKm { get; set; }
    public decimal Subtotal { get; set; }

    // Additional fields for pickup point transaction
    public string ParentEmail { get; set; } = null!;
    public string SemesterName { get; set; } = null!;
    public string SemesterCode { get; set; } = null!;
    public string AcademicYear { get; set; } = null!;
    public DateTime SemesterStartDate { get; set; }
    public DateTime SemesterEndDate { get; set; }
    public TransportFeeItemType Type { get; set; } = TransportFeeItemType.Register;

    public TransportFeeItemStatus Status { get; set; }
    public Guid? TransactionId { get; set; }
    public Guid? UnitPriceId { get; set; }

    [JsonIgnore]
    public virtual Student Student { get; set; } = null!;
    [JsonIgnore]
    public virtual Transaction? Transaction { get; set; }
    [JsonIgnore]
    public virtual UnitPrice? UnitPrice { get; set; }
}
