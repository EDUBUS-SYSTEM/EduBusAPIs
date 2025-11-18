namespace Services.Models.SupervisorVehicle
{
    public class UpdateSupervisorAssignmentRequest
    {
        public DateTime? StartTimeUtc { get; set; }
        public DateTime? EndTimeUtc { get; set; }
        public string? AssignmentReason { get; set; }
    }
}
