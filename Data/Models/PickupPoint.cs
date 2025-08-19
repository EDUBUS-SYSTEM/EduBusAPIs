using NetTopologySuite.Geometries;

namespace Data.Models;

public partial class PickupPoint : BaseDomain
{
    public string Description { get; set; } = null!;

    public string Location { get; set; } = null!;

    public Point? Geog { get; set; }

    public virtual ICollection<StudentPickupPoint> StudentPickupPoints { get; set; } = new List<StudentPickupPoint>();
}
