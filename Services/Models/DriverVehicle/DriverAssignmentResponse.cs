namespace Services.Models.DriverVehicle
{
    public class DriverAssignmentResponse
    {
        public bool Success { get; set; }
        public DriverAssignmentDto? Data { get; set; }
        public object? Error { get; set; }
    }
}
