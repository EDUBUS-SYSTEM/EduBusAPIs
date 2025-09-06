using Data.Models.Enums;

namespace Data.Models;

public partial class DriverVehicle : BaseDomain
{
    public Guid DriverId { get; set; }

    public Guid VehicleId { get; set; }

    public bool IsPrimaryDriver { get; set; }

    public DateTime StartTimeUtc { get; set; }

    public DateTime? EndTimeUtc { get; set; }

    public DriverVehicleStatus Status { get; set; } = DriverVehicleStatus.Pending;
    public string? AssignmentReason { get; set; }
    public Guid AssignedByAdminId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedByAdminId { get; set; }
    public string? ApprovalNote { get; set; }

    public virtual Driver Driver { get; set; } = null!;

    public virtual Vehicle Vehicle { get; set; } = null!;
    public virtual Admin AssignedByAdmin { get; set; } = null!;
    public virtual Admin? ApprovedByAdmin { get; set; }
}
