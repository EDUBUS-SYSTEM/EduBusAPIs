namespace Data.Models;

public partial class Admin : UserAccount
{
    public virtual ICollection<UnitPrice> UnitPrices { get; set; } = new List<UnitPrice>();
    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    public virtual ICollection<DriverVehicle> AssignedDriverVehicles { get; set; } = new List<DriverVehicle>();
    public virtual ICollection<DriverVehicle> ApprovedDriverVehicles { get; set; } = new List<DriverVehicle>();
    public virtual ICollection<DriverLeaveRequest> ApprovedLeaveRequests { get; set; } = new List<DriverLeaveRequest>();
}
