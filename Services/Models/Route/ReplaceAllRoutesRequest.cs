using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Models.Route
{
	public class ReplaceAllRoutesRequest
	{
		[Required(ErrorMessage = "Routes list is required.")]
		[MinLength(1, ErrorMessage = "At least one route must be provided.")]
		[MaxLength(50, ErrorMessage = "Cannot create more than 50 routes at once.")]
		public List<CreateRouteRequest> Routes { get; set; } = new List<CreateRouteRequest>();

		public RouteScheduleRequest? RouteSchedule { get; set; }

		/// <summary>
		/// If true, will force delete all routes even if some are in use by active trips
		/// </summary>
		public bool ForceDelete { get; set; } = false;
	}

	public class ReplaceAllRoutesResponse
	{
		public bool Success { get; set; }
		public int DeletedRoutes { get; set; }
		public int TotalNewRoutes { get; set; }
		public int SuccessfulRoutes { get; set; }
		public int FailedRoutes { get; set; }
		public List<RouteDto> CreatedRoutes { get; set; } = new List<RouteDto>();
		public List<BulkRouteError> Errors { get; set; } = new List<BulkRouteError>();
		public string? ErrorMessage { get; set; }
	}
}
