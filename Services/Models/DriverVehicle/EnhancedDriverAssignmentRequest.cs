using System.ComponentModel.DataAnnotations;

namespace Services.Models.DriverVehicle
{
    public class EnhancedDriverAssignmentRequest
    {
        [Required(ErrorMessage = "Driver ID is required.")]
        public Guid DriverId { get; set; }
        
        public bool IsPrimaryDriver { get; set; }
        
        [Required(ErrorMessage = "Start time is required.")]
        public DateTime StartTimeUtc { get; set; }
        
        public DateTime? EndTimeUtc { get; set; }
        
        [StringLength(500, ErrorMessage = "Assignment reason cannot exceed 500 characters.")]
        public string? AssignmentReason { get; set; }
        
        public bool RequireApproval { get; set; } = true;
        
        [StringLength(1000, ErrorMessage = "Additional notes cannot exceed 1000 characters.")]
        public string? AdditionalNotes { get; set; }
        
        // Emergency assignment flag
        public bool IsEmergencyAssignment { get; set; } = false;
        
        // Priority level for assignment
        public int PriorityLevel { get; set; } = 1; // 1 = Normal, 2 = High, 3 = Emergency
    }
}

