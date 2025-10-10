using System.ComponentModel.DataAnnotations;

namespace Services.Models.Route
{
    public class CreateRouteRequest
    {
        [Required(ErrorMessage = "Route name is required.")]
        [MaxLength(200, ErrorMessage = "Route name cannot exceed 200 characters.")]
        public string RouteName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vehicle ID is required.")]
        public Guid VehicleId { get; set; }

        public List<RoutePickupPointRequest> PickupPoints { get; set; } = new List<RoutePickupPointRequest>();

        public RouteScheduleRequest? RouteSchedule { get; set; }
    }

    public class RoutePickupPointRequest
    {
        [Required(ErrorMessage = "Pickup point ID is required.")]
        public Guid PickupPointId { get; set; }

        [Required(ErrorMessage = "Sequence order is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Sequence order must be a positive number.")]
        public int SequenceOrder { get; set; }
    }

    
}
