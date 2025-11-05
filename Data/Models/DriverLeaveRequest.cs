using Data.Models.Enums;

namespace Data.Models;

public class DriverLeaveRequest : BaseDomain
{
    public Guid DriverId { get; set; }
    public LeaveType LeaveType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    
    // Approval fields
    public Guid? ApprovedByAdminId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovalNote { get; set; }
    
    // Auto-replacement fields
    public bool AutoReplacementEnabled { get; set; } = true;
    public Guid? SuggestedReplacementDriverId { get; set; }
    public Guid? SuggestedReplacementVehicleId { get; set; }
    public DateTime? SuggestionGeneratedAt { get; set; }
    
    // Navigation properties
    public virtual Driver Driver { get; set; } = null!;
    public virtual Admin? ApprovedByAdmin { get; set; }
    public virtual Driver? SuggestedReplacementDriver { get; set; }
    public virtual Vehicle? SuggestedReplacementVehicle { get; set; }
}
