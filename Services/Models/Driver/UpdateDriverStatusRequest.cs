using Data.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Services.Models.Driver
{
    public class UpdateDriverStatusRequest
    {
        [Required(ErrorMessage = "Status is required.")]
        public DriverStatus Status { get; set; }
        
        [StringLength(500, ErrorMessage = "Note cannot exceed 500 characters.")]
        public string? Note { get; set; }
        
        public DateTime? EffectiveFrom { get; set; }
        
        public DateTime? EffectiveTo { get; set; }
    }
}
