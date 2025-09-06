using Data.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Services.Models.Driver
{
    public class UpdateLeaveRequestDto
    {
        public LeaveType? LeaveType { get; set; }
        
        public DateTime? StartDate { get; set; }
        
        public DateTime? EndDate { get; set; }
        
        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
        public string? Reason { get; set; }
        
        public bool? AutoReplacementEnabled { get; set; }
        
        [StringLength(1000, ErrorMessage = "Additional information cannot exceed 1000 characters.")]
        public string? AdditionalInformation { get; set; }
    }
}
