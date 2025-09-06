using NetTopologySuite.Geometries;

namespace Data.Models;

public partial class PickupPointRequest : BaseDomain
{
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
    public string AdminNotes { get; set; } = string.Empty;
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedByAdminId { get; set; }
    public Guid RequestedByParentId { get; set; }

    public Point Geog { get; set; }

    // Navigation properties
    public virtual Parent RequestedByParent { get; set; } = null!;
    public virtual Admin? ReviewedByAdmin { get; set; }
}

