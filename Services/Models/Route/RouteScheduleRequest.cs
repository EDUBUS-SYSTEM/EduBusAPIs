using System.ComponentModel.DataAnnotations;

namespace Services.Models.Route
{
    public class RouteScheduleRequest
    {
        [Required(ErrorMessage = "Schedule ID is required.")]
        public Guid ScheduleId { get; set; }

        [Required(ErrorMessage = "Effective from date is required.")]
        public DateTime EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Priority must be a non-negative number.")]
        public int Priority { get; set; } = 0;
    }
}
