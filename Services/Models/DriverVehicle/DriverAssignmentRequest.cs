namespace Services.Models.DriverVehicle
{
    public class DriverAssignmentRequest
    {
        public Guid DriverId { get; set; }
        public bool IsPrimaryDriver { get; set; }
        public DateTime StartTimeUtc { get; set; }
        public DateTime? EndTimeUtc { get; set; }
    }
}
