namespace Services.Models.DriverVehicle
{
    /// <summary>
    /// Response for vehicle students API
    /// </summary>
    public class VehicleStudentsResponse
    {
        public bool Success { get; set; }
        public VehicleStudentsData? Data { get; set; }
        public string? Error { get; set; }
        public string? Message { get; set; }
    }
}

