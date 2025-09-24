namespace Services.Models.DriverVehicle
{
    public class DriverAssignmentSummaryResponse
    {
        public bool Success { get; set; }
        public DriverAssignmentSummaryDto Data { get; set; } = new DriverAssignmentSummaryDto();
        public object? Error { get; set; }
    }
}
