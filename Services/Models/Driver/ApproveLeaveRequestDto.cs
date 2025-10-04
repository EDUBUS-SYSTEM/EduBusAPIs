using System.ComponentModel.DataAnnotations;

namespace Services.Models.Driver
{
    public class ApproveLeaveRequestDto
    {
        //[Required(ErrorMessage = "Approval decision is required.")]
        //public bool IsApproved { get; set; }
        
        [StringLength(500, ErrorMessage = "Note cannot exceed 500 characters.")]
        public string? Notes { get; set; }
        
        /// <summary>
        /// Optional replacement driver ID for the leave period
        /// </summary>
        public Guid? ReplacementDriverId { get; set; }
        
        // TODO: These properties are currently not being used effectively
        // EffectiveFrom and EffectiveTo are only used for validation but don't provide real value
        // Consider removing or implementing proper functionality
        // public DateTime? EffectiveFrom { get; set; }
        
        // public DateTime? EffectiveTo { get; set; }
        
        // TODO: AdditionalConditions is defined but not implemented in the service layer
        // Consider implementing storage and usage of additional conditions
        // [StringLength(500, ErrorMessage = "Additional conditions cannot exceed 500 characters.")]
        // public string? AdditionalConditions { get; set; }
    }
}
