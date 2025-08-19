namespace Data.Models;

public partial class Driver : UserAccount
{
    public byte[] HashedLicenseNumber { get; set; } = null!;

    public virtual ICollection<DriverVehicle> DriverVehicles { get; set; } = new List<DriverVehicle>();
}
