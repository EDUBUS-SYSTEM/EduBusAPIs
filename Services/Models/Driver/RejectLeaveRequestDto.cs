using System.ComponentModel.DataAnnotations;

namespace Services.Models.Driver
{
    public class RejectLeaveRequestDto
    {
        [Required(ErrorMessage = "Rejection reason is required.")]
        [StringLength(500, ErrorMessage = "Rejection reason cannot exceed 500 characters.")]
        public string Reason { get; set; } = string.Empty;
        
        // TODO: AdditionalNotes is defined but not implemented in the service layer
        // Consider implementing storage and usage of additional notes
        // [StringLength(500, ErrorMessage = "Additional notes cannot exceed 500 characters.")]
        // public string? AdditionalNotes { get; set; }
        
        // TODO: SuggestedAlternativeStartDate and SuggestedAlternativeEndDate have validation logic
        // but are not stored in database or used in notifications
        // Consider implementing proper functionality or removing
        // public DateTime? SuggestedAlternativeStartDate { get; set; }
        
        // public DateTime? SuggestedAlternativeEndDate { get; set; }
    }
}
