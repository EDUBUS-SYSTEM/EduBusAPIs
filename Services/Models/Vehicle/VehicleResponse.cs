namespace Services.Models.Vehicle
{
    public class VehicleResponse
    {
        public bool Success { get; set; }
        public VehicleDto? Data { get; set; }
        public object? Error { get; set; }
    }
}
