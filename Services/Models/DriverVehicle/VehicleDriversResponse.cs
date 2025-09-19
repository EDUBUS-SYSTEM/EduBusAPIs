namespace Services.Models.DriverVehicle
{
    public class VehicleDriversResponse
    {
        public bool Success { get; set; }
        public IEnumerable<DriverAssignmentDto> Data { get; set; } = new List<DriverAssignmentDto>();
        public object? Error { get; set; }
    }
}
