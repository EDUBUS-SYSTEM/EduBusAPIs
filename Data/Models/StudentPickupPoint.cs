namespace Data.Models;

public partial class StudentPickupPoint : BaseDomain
{
    public Guid StudentId { get; set; }

    public Guid PickupPointId { get; set; }

    public DateTime StartTimeUtc { get; set; }

    public DateTime? EndTimeUtc { get; set; }

    public bool IsActive { get; set; }

    public virtual PickupPoint PickupPoint { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
