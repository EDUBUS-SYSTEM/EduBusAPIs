using System.ComponentModel.DataAnnotations;

namespace Services.Models.Driver
{
    public class ApproveLeaveRequestDto
    {
        [Required(ErrorMessage = "Approval decision is required.")]
        public bool IsApproved { get; set; }
        
        [StringLength(500, ErrorMessage = "Note cannot exceed 500 characters.")]
        public string? Note { get; set; }
        
        public DateTime? EffectiveFrom { get; set; }
        
        public DateTime? EffectiveTo { get; set; }
        
        [StringLength(500, ErrorMessage = "Additional conditions cannot exceed 500 characters.")]
        public string? AdditionalConditions { get; set; }
    }
}
