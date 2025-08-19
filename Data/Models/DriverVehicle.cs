namespace Data.Models;

public partial class DriverVehicle : BaseDomain
{
    public Guid DriverId { get; set; }

    public Guid VehicleId { get; set; }

    public bool IsPrimaryDriver { get; set; }

    public DateTime StartTimeUtc { get; set; }

    public DateTime? EndTimeUtc { get; set; }

    public virtual Driver Driver { get; set; } = null!;

    public virtual Vehicle Vehicle { get; set; } = null!;
}
