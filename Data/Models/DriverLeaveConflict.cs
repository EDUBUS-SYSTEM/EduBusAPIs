using Data.Models.Enums;

namespace Data.Models;

public class DriverLeaveConflict : BaseDomain
{
    public Guid LeaveRequestId { get; set; }
    public Guid TripId { get; set; }
    public DateTime TripStartTime { get; set; }
    public DateTime TripEndTime { get; set; }
    public string RouteName { get; set; } = string.Empty;
    public int AffectedStudents { get; set; }
    public ConflictSeverity Severity { get; set; }
    
    // Replacement suggestion
    public Guid? SuggestedDriverId { get; set; }
    public Guid? SuggestedVehicleId { get; set; }
    public double ReplacementScore { get; set; } // 0-100
    public string ReplacementReason { get; set; } = string.Empty;
    
    public virtual DriverLeaveRequest LeaveRequest { get; set; } = null!;
    public virtual Trip Trip { get; set; } = null!;
    public virtual Driver? SuggestedDriver { get; set; }
    public virtual Vehicle? SuggestedVehicle { get; set; }
}
