using Data.Models.Enums;

namespace Services.Models.DriverVehicle
{
    public class EnhancedDriverAssignmentResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DriverAssignmentDto? Data { get; set; }
        
        // Assignment details
        public Guid AssignmentId { get; set; }
        public Guid DriverId { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public Guid VehicleId { get; set; }
        public string VehiclePlate { get; set; } = string.Empty;
        public DriverVehicleStatus Status { get; set; }
        
        // Time information
        public DateTime StartTimeUtc { get; set; }
        public DateTime? EndTimeUtc { get; set; }
        public bool IsPrimaryDriver { get; set; }
        
        // Approval information
        public bool RequireApproval { get; set; }
        public bool IsApproved { get; set; }
        public Guid? ApprovedByAdminId { get; set; }
        public string? ApprovedByAdminName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        
        // Additional information
        public string? AssignmentReason { get; set; }
        public string? AdditionalNotes { get; set; }
        public bool IsEmergencyAssignment { get; set; }
        public int PriorityLevel { get; set; }
        
        // Validation results
        public List<string> ValidationWarnings { get; set; } = new List<string>();
        public List<string> ValidationErrors { get; set; } = new List<string>();
        public bool HasWarnings => ValidationWarnings.Any();
        public bool HasErrors => ValidationErrors.Any();
    }
}

