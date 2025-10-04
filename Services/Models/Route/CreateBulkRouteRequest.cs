using System.ComponentModel.DataAnnotations;

namespace Services.Models.Route
{
    public class CreateBulkRouteRequest
    {
        [Required(ErrorMessage = "Routes list is required.")]
        [MinLength(1, ErrorMessage = "At least one route must be provided.")]
        [MaxLength(50, ErrorMessage = "Cannot create more than 50 routes at once.")]
        public List<CreateRouteRequest> Routes { get; set; } = new List<CreateRouteRequest>();

        public RouteScheduleRequest? RouteSchedule { get; set; }
    }

    public class CreateBulkRouteResponse
    {
        public bool Success { get; set; }
        public int TotalRoutes { get; set; }
        public int SuccessfulRoutes { get; set; }
        public int FailedRoutes { get; set; }
        public List<RouteDto> CreatedRoutes { get; set; } = new List<RouteDto>();
        public List<BulkRouteError> Errors { get; set; } = new List<BulkRouteError>();
    }

    public class BulkRouteError
    {
        public int Index { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
    }
}