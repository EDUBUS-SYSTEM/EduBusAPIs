namespace Services.Models.DriverVehicle
{
    /// <summary>
    /// Response for driver's current vehicle information
    /// </summary>
    public class DriverVehicleInfoResponse
    {
        public bool Success { get; set; }
        public DriverVehicleInfoDto? Data { get; set; }
        public string? Error { get; set; }
        public string? Message { get; set; }
    }
}

