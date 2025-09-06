using Data.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Services.Models.Driver
{
    public class CreateLeaveRequestDto
    {
        public Guid DriverId { get; set; }
        
        [Required(ErrorMessage = "Leave type is required.")]
        public LeaveType LeaveType { get; set; }
        
        [Required(ErrorMessage = "Start date is required.")]
        public DateTime StartDate { get; set; }
        
        [Required(ErrorMessage = "End date is required.")]
        public DateTime EndDate { get; set; }
        
        [Required(ErrorMessage = "Reason is required.")]
        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
        public string Reason { get; set; } = string.Empty;
        
        public bool AutoReplacementEnabled { get; set; } = true;
        
        [StringLength(1000, ErrorMessage = "Additional information cannot exceed 1000 characters.")]
        public string? AdditionalInformation { get; set; }
    }
}
