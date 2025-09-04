using System.ComponentModel.DataAnnotations;

namespace Services.Models.Driver
{
    public class SuspendDriverRequest
    {
        [Required(ErrorMessage = "Reason is required.")]
        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
        public string Reason { get; set; } = string.Empty;
        
        public DateTime? UntilDate { get; set; }
        
        [StringLength(500, ErrorMessage = "Additional notes cannot exceed 500 characters.")]
        public string? AdditionalNotes { get; set; }
    }
}
