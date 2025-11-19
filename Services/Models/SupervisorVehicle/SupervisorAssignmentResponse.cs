namespace Services.Models.SupervisorVehicle
{
    public class SupervisorAssignmentResponse
    {
        public bool Success { get; set; }
        public SupervisorAssignmentDto? Data { get; set; }
        public string? Error { get; set; }
        public string? Message { get; set; }
    }
}
