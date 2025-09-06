using System.ComponentModel.DataAnnotations;

namespace Services.Models.Driver
{
    public class RejectLeaveRequestDto
    {
        [Required(ErrorMessage = "Rejection reason is required.")]
        [StringLength(500, ErrorMessage = "Rejection reason cannot exceed 500 characters.")]
        public string Reason { get; set; } = string.Empty;
        
        [StringLength(500, ErrorMessage = "Additional notes cannot exceed 500 characters.")]
        public string? AdditionalNotes { get; set; }
        
        public DateTime? SuggestedAlternativeStartDate { get; set; }
        
        public DateTime? SuggestedAlternativeEndDate { get; set; }
    }
}
