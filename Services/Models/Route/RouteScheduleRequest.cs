using System.ComponentModel.DataAnnotations;

namespace Services.Models.Route
{
    public class RouteScheduleRequest
    {
        [Required(ErrorMessage = "Schedule ID is required.")]
        public Guid ScheduleId { get; set; }

        public DateTime? EffectiveFrom { get; set; } = null; // null = use DateTime.UtcNow.Date

        public DateTime? EffectiveTo { get; set; } = null; // null = inherit from Schedule

        [Range(0, int.MaxValue, ErrorMessage = "Priority must be a non-negative number.")]
        public int Priority { get; set; } = 0;
    }
}
