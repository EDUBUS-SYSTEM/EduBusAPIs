using Data.Models.Enums;

namespace Services.Models.DriverVehicle
{
    public class DriverAssignmentSummaryDto
    {
        public Guid DriverId { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public string DriverEmail { get; set; } = string.Empty;
        public DriverStatus DriverStatus { get; set; }
        
        // Current assignments
        public List<DriverAssignmentDto> CurrentAssignments { get; set; } = new List<DriverAssignmentDto>();
        public int TotalCurrentAssignments { get; set; }
        public bool HasActiveAssignments { get; set; }
        
        // Assignment statistics
        public int TotalAssignments { get; set; }
        public int CompletedAssignments { get; set; }
        public int CancelledAssignments { get; set; }
        public int PendingAssignments { get; set; }
        
        // Time statistics
        public TimeSpan TotalWorkingHours { get; set; }
        public DateTime? LastAssignmentDate { get; set; }
        public DateTime? NextAssignmentDate { get; set; }
        
        // Vehicle information
        public List<Guid> AssignedVehicleIds { get; set; } = new List<Guid>();
        public int TotalVehiclesAssigned { get; set; }
        
        // Performance metrics
        public double AssignmentCompletionRate { get; set; } // Percentage
        public int OnTimeAssignments { get; set; }
        public int LateAssignments { get; set; }
        public double PunctualityRate { get; set; } // Percentage
    }
}

