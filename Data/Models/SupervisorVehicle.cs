using Data.Models.Enums;

namespace Data.Models;

public partial class SupervisorVehicle : BaseDomain
{
    public Guid SupervisorId { get; set; }

    public Guid VehicleId { get; set; }

    public DateTime StartTimeUtc { get; set; }

    public DateTime? EndTimeUtc { get; set; }

    public SupervisorVehicleStatus Status { get; set; } = SupervisorVehicleStatus.Assigned;
    
    public string? AssignmentReason { get; set; }
    
    public Guid AssignedByAdminId { get; set; }
    
    public DateTime? ApprovedAt { get; set; }
    
    public Guid? ApprovedByAdminId { get; set; }
    
    public string? ApprovalNote { get; set; }

    public virtual Supervisor Supervisor { get; set; } = null!;

    public virtual Vehicle Vehicle { get; set; } = null!;
    
    public virtual Admin AssignedByAdmin { get; set; } = null!;
    
    public virtual Admin? ApprovedByAdmin { get; set; }
}
