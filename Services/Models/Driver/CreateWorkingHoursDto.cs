using System.ComponentModel.DataAnnotations;

namespace Services.Models.Driver
{
    public class CreateWorkingHoursDto
    {
        [Required]
        public Guid DriverId { get; set; }
        
        [Required]
        [Range(0, 6, ErrorMessage = "DayOfWeek must be between 0 (Sunday) and 6 (Saturday).")]
        public DayOfWeek DayOfWeek { get; set; }
        
        [Required]
        public TimeSpan StartTime { get; set; }
        
        [Required]
        public TimeSpan EndTime { get; set; }
        
        public bool IsAvailable { get; set; } = true;
    }
}
