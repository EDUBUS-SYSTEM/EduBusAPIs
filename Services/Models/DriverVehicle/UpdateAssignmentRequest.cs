using System.ComponentModel.DataAnnotations;

namespace Services.Models.DriverVehicle
{
    public class UpdateAssignmentRequest
    {
        public bool? IsPrimaryDriver { get; set; }
        
        public DateTime? StartTimeUtc { get; set; }
        
        public DateTime? EndTimeUtc { get; set; }
        
        [StringLength(500, ErrorMessage = "Assignment reason cannot exceed 500 characters.")]
        public string? AssignmentReason { get; set; }
        
        [StringLength(1000, ErrorMessage = "Additional notes cannot exceed 1000 characters.")]
        public string? AdditionalNotes { get; set; }
        
        public int? PriorityLevel { get; set; }
        
        [StringLength(500, ErrorMessage = "Update reason cannot exceed 500 characters.")]
        public string? UpdateReason { get; set; }
    }
}

