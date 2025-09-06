using Data.Models.Enums;

namespace Services.Models.DriverVehicle
{
    public class AssignmentConflictDto
    {
        public Guid ConflictId { get; set; }
        public string ConflictType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ConflictSeverity Severity { get; set; }
        public DateTime ConflictTime { get; set; }
        public string? Resolution { get; set; }
        
        // Assignment details
        public Guid? ConflictingAssignmentId { get; set; }
        public Guid? ConflictingDriverId { get; set; }
        public string? ConflictingDriverName { get; set; }
        public Guid? ConflictingVehicleId { get; set; }
        public string? ConflictingVehiclePlate { get; set; }
        
        // Time conflict details
        public DateTime? OverlapStartTime { get; set; }
        public DateTime? OverlapEndTime { get; set; }
        public TimeSpan? OverlapDuration { get; set; }
        
        // Resolution suggestions
        public List<string> SuggestedResolutions { get; set; } = new List<string>();
        public bool IsResolvable { get; set; }
        public string? ResolutionNotes { get; set; }
    }
}

