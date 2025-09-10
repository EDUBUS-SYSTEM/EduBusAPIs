namespace Data.Models;

public partial class StudentPickupPointHistory : BaseDomain
{
    public Guid StudentId { get; set; }
    public Guid PickupPointId { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? RemovedAt { get; set; }
    public string ChangeReason { get; set; } = string.Empty;
    public string ChangedBy { get; set; } = string.Empty;
    public virtual Student Student { get; set; } = null!;
    public virtual PickupPoint PickupPoint { get; set; } = null!;
}
