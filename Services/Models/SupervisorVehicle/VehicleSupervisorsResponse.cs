namespace Services.Models.SupervisorVehicle
{
    public class VehicleSupervisorsResponse
    {
        public bool Success { get; set; }
        public List<SupervisorAssignmentDto> Data { get; set; } = new List<SupervisorAssignmentDto>();
        public string? Error { get; set; }
    }
}
