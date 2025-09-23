using System.ComponentModel.DataAnnotations;

namespace Services.Models.Route
{
    public class UpdateRouteRequest
    {
        [MaxLength(200, ErrorMessage = "Route name cannot exceed 200 characters.")]
        public string? RouteName { get; set; }

        public Guid? VehicleId { get; set; }

        public List<RoutePickupPointRequest>? PickupPoints { get; set; }
    }
}
