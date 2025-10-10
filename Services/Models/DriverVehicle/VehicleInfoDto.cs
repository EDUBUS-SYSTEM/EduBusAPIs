namespace Services.Models.DriverVehicle
{
    public class VehicleInfoDto
    {
        public Guid Id { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
