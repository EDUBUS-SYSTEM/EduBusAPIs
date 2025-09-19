namespace Services.Models.DriverVehicle
{
    public class DriverAssignmentDto
    {
        public Guid Id { get; set; }
        public Guid DriverId { get; set; }
        public Guid VehicleId { get; set; }
        public bool IsPrimaryDriver { get; set; }
        public DateTime StartTimeUtc { get; set; }
        public DateTime? EndTimeUtc { get; set; }
        public DriverInfoDto? Driver { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid AssignedByAdminId { get; set; }
    }
}
