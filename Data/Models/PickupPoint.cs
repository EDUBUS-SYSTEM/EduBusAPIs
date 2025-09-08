using NetTopologySuite.Geometries;

namespace Data.Models;

public partial class PickupPoint : BaseDomain
{
    public string Description { get; set; } = null!;

    public string Location { get; set; } = null!;

    public Point Geog { get; set; }

    // Students currently assigned to this pickup point
    public virtual ICollection<Student> Students { get; set; } = new List<Student>();

    // History of all student assignments to this pickup point
    public virtual ICollection<StudentPickupPointHistory> StudentPickupPointHistory { get; set; } = new List<StudentPickupPointHistory>();
}
