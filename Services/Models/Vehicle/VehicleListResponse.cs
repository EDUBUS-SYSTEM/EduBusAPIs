namespace Services.Models.Vehicle
{
    public class VehicleListResponse
    {
        public bool Success { get; set; }
        public IEnumerable<VehicleDto> Data { get; set; } = new List<VehicleDto>();
        public object? Error { get; set; }
    }
}