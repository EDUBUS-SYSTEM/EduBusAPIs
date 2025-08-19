namespace Data.Models;

public partial class Vehicle : BaseDomain
{
    public byte[] HashedLicensePlate { get; set; } = null!;

    public int Capacity { get; set; }

    public string Status { get; set; } = null!;

    public Guid AdminId { get; set; }

    public virtual Admin Admin { get; set; } = null!;

    public virtual DriverVehicle? DriverVehicle { get; set; }
}
