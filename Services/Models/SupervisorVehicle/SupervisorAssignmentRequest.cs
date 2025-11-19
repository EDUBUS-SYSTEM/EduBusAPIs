namespace Services.Models.SupervisorVehicle
{
    public class SupervisorAssignmentRequest
    {
        public Guid SupervisorId { get; set; }
        public DateTime StartTimeUtc { get; set; }
        public DateTime? EndTimeUtc { get; set; }
        public string? AssignmentReason { get; set; }
    }
}
