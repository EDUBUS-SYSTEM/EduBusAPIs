using Data.Models.Enums;

namespace Services.Models.Driver
{
    public class DriverLeaveConflictDto
    {
        public Guid Id { get; set; }
        public Guid LeaveRequestId { get; set; }
        public Guid TripId { get; set; }
        public DateTime TripStartTime { get; set; }
        public DateTime TripEndTime { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public int AffectedStudents { get; set; }
        public ConflictSeverity Severity { get; set; }
        
        // Replacement suggestion
        public Guid? SuggestedDriverId { get; set; }
        public string? SuggestedDriverName { get; set; }
        public Guid? SuggestedVehicleId { get; set; }
        public string? SuggestedVehiclePlate { get; set; }
        public double ReplacementScore { get; set; } // 0-100
        public string ReplacementReason { get; set; } = string.Empty;
        
        // Conflict details
        public string ConflictDescription { get; set; } = string.Empty;
        public DateTime ConflictDetectedAt { get; set; }
        public bool IsResolved { get; set; }
    }
}
