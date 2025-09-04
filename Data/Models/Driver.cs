using Data.Models.Enums;

namespace Data.Models;

public partial class Driver : UserAccount
{
    public Guid? HealthCertificateFileId { get; set; }
    public DriverStatus Status { get; set; } = DriverStatus.Active;
    public DateTime? LastActiveDate { get; set; }
    public string? StatusNote { get; set; }

    public virtual ICollection<DriverVehicle> DriverVehicles { get; set; } = new List<DriverVehicle>();
    
    public virtual DriverLicense? DriverLicense { get; set; }
    public virtual ICollection<DriverLeaveRequest> LeaveRequests { get; set; } = new List<DriverLeaveRequest>();
}
