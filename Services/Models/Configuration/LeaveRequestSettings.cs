using System.ComponentModel.DataAnnotations;

namespace Services.Models.Configuration
{
    public class LeaveRequestSettings
    {
        [Range(0, 168, ErrorMessage = "Minimum advance notice hours must be between 0 and 168 (1 week).")]
        public int MinimumAdvanceNoticeHours { get; set; } = 12;
        
        public bool AllowEmergencyLeaveRequests { get; set; } = true;
        
        [Range(0, 168, ErrorMessage = "Emergency leave advance notice hours must be between 0 and 168 (1 week).")]
        public int EmergencyLeaveAdvanceNoticeHours { get; set; } = 2;
    }
}
