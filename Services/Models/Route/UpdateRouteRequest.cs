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

	public class UpdateBulkRouteRequest
	{
		[Required(ErrorMessage = "Routes list is required.")]
		[MinLength(1, ErrorMessage = "At least one route must be provided.")]
		[MaxLength(50, ErrorMessage = "Cannot update more than 50 routes at once.")]
		public List<UpdateBulkRouteItem> Routes { get; set; } = new List<UpdateBulkRouteItem>();
	}

	public class UpdateBulkRouteItem
	{
		[Required(ErrorMessage = "Route ID is required.")]
		public Guid RouteId { get; set; }

		[MaxLength(200, ErrorMessage = "Route name cannot exceed 200 characters.")]
		public string? RouteName { get; set; }

		public Guid? VehicleId { get; set; }

		public List<RoutePickupPointRequest>? PickupPoints { get; set; }
	}

	public class UpdateBulkRouteResponse
	{
		public bool Success { get; set; }
		public int TotalRoutes { get; set; }
		public List<RouteDto> UpdatedRoutes { get; set; } = new List<RouteDto>();
		public string? ErrorMessage { get; set; }
	}
}
