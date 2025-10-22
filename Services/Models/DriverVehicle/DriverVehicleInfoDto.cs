using Data.Models.Enums;

namespace Services.Models.DriverVehicle
{
    /// <summary>
    /// DTO for driver's current vehicle information
    /// </summary>
    public class DriverVehicleInfoDto
    {
        public Guid VehicleId { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public VehicleStatus Status { get; set; }
        public string? StatusNote { get; set; }
        public bool IsPrimaryDriver { get; set; }
        public DateTime AssignmentStartTime { get; set; }
        public DateTime? AssignmentEndTime { get; set; }
        public DriverVehicleStatus AssignmentStatus { get; set; }
    }
}

