using Data.Models.Enums;

namespace Data.Models;

public partial class Vehicle : BaseDomain
{
    public byte[] HashedLicensePlate { get; set; } = null!;

    public int Capacity { get; set; }

    public VehicleStatus Status { get; set; } = VehicleStatus.Active;
    public string? StatusNote { get; set; }

    public Guid AdminId { get; set; }

    public virtual Admin Admin { get; set; } = null!;
    public virtual ICollection<DriverVehicle> DriverVehicles { get; set; } = new List<DriverVehicle>();
}
