using Data.Models.Enums;

namespace Services.Models.SupervisorVehicle
{
    public class SupervisorAssignmentDto
    {
        public Guid Id { get; set; }
        public Guid SupervisorId { get; set; }
        public string SupervisorName { get; set; } = string.Empty;
        public string SupervisorEmail { get; set; } = string.Empty;
        public string SupervisorPhone { get; set; } = string.Empty;
        public Guid VehicleId { get; set; }
        public string VehiclePlate { get; set; } = string.Empty;
        public int VehicleCapacity { get; set; }
        public DateTime StartTimeUtc { get; set; }
        public DateTime? EndTimeUtc { get; set; }
        public SupervisorVehicleStatus Status { get; set; }
        public string? AssignmentReason { get; set; }
        public Guid AssignedByAdminId { get; set; }
        public string? AssignedByAdminName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public Guid? ApprovedByAdminId { get; set; }
        public string? ApprovedByAdminName { get; set; }
        public string? ApprovalNote { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
