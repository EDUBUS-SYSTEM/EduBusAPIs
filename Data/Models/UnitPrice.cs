namespace Data.Models;

public partial class UnitPrice : BaseDomain
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = string.Empty;

    public decimal PricePerKm { get; set; }

    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; } = null;

    public bool IsActive { get; set; } = true;

    // Admin 
    public Guid ByAdminId { get; set; }
    public string ByAdminName { get; set; } = string.Empty;

    // Navigation properties
    public virtual Admin ByAdmin { get; set; } = null!;
    public virtual ICollection<TransportFeeItem> TransportFeeItems { get; set; } = new List<TransportFeeItem>();
}