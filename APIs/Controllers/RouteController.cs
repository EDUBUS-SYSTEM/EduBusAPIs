using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.Route;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoutesController : ControllerBase
    {
        private readonly IRouteService _routeService;
        private readonly IRouteSuggestionService _routeSuggestionService;

        public RoutesController(
            IRouteService routeService,
            IRouteSuggestionService routeSuggestionService)
        {
            _routeService = routeService;
            _routeSuggestionService = routeSuggestionService;
        }

        [HttpGet("suggestions")]
        public async Task<ActionResult<RouteSuggestionResponse>> GenerateRouteSuggestions()
        {
            try
            {
                var response = await _routeSuggestionService.GenerateRouteSuggestionsAsync();

                return response.Success ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("{routeId:guid}/optimize")]
        public async Task<ActionResult<RouteSuggestionResponse>> OptimizeExistingRoute(
            [FromRoute] Guid routeId)
        {
            try
            {
                if (routeId == Guid.Empty)
                    return BadRequest("Invalid route ID");

                var response = await _routeSuggestionService.OptimizeExistingRouteAsync(routeId);

                return response.Success ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        #region CRUD Operations

        [HttpPost]
        public async Task<ActionResult<RouteDto>> CreateRoute([FromBody] CreateRouteRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var route = await _routeService.CreateRouteAsync(request);
                return CreatedAtAction(nameof(GetRouteById), new { id = route.Id }, route);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Create multiple routes at once
        /// </summary>
        /// <param name="request">Bulk route creation request</param>
        /// <returns>Bulk creation result</returns>
        [HttpPost("bulk")]
        public async Task<ActionResult<CreateBulkRouteResponse>> CreateBulkRoutes([FromBody] CreateBulkRouteRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var response = await _routeService.CreateBulkRoutesAsync(request);
                
                if (response.Success)
                {
                    return Ok(response);
                }
                else if (response.SuccessfulRoutes > 0)
                {
                    // Partial success - some routes created successfully
                    return StatusCode(207, response); // Multi-Status
                }
                else
                {
                    // All routes failed
                    return BadRequest(response);
                }
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<RouteDto>> GetRouteById([FromRoute] Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                    return BadRequest("Invalid route ID");

                var route = await _routeService.GetRouteByIdAsync(id);
                if (route == null)
                    return NotFound("Route not found");

                return Ok(route);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RouteDto>>> GetAllRoutes()
        {
            try
            {
                var routes = await _routeService.GetAllRoutesAsync();
                return Ok(routes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<RouteDto>>> GetActiveRoutes()
        {
            try
            {
                var routes = await _routeService.GetActiveRoutesAsync();
                return Ok(routes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<RouteDto>> UpdateRoute([FromRoute] Guid id, [FromBody] UpdateRouteRequest request)
        {
            try
            {
                if (id == Guid.Empty)
                    return BadRequest("Invalid route ID");

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var route = await _routeService.UpdateRouteAsync(id, request);
                if (route == null)
                    return NotFound("Route not found");

                return Ok(route);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error" });
            }
        }


		/// <summary>
		/// Update route basic information (name and vehicle only) without modifying pickup points
		/// </summary>
		/// <param name="id">Route ID</param>
		/// <param name="request">Basic route update request</param>
		/// <returns>Updated route</returns>
		[HttpPut("{id:guid}/basic")]
		public async Task<ActionResult<RouteDto>> UpdateRouteBasic([FromRoute] Guid id, [FromBody] UpdateRouteBasicRequest request)
		{
			try
			{
				if (id == Guid.Empty)
					return BadRequest("Invalid route ID");

				if (!ModelState.IsValid)
					return BadRequest(ModelState);

				var route = await _routeService.UpdateRouteBasicAsync(id, request);
				if (route == null)
					return NotFound("Route not found");

				return Ok(route);
			}
			catch (ArgumentException ex)
			{
				return BadRequest(ex.Message);
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(ex.Message);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Internal server error" });
			}
		}

		/// <summary>
		/// Update multiple routes at once (all or nothing)
		/// </summary>
		/// <param name="request">Bulk route update request</param>
		/// <returns>Bulk update result</returns>
		[HttpPut("bulk")]
		public async Task<ActionResult<UpdateBulkRouteResponse>> UpdateBulkRoutes([FromBody] UpdateBulkRouteRequest request)
		{
			try
			{
				if (!ModelState.IsValid)
					return BadRequest(ModelState);

				var response = await _routeService.UpdateBulkRoutesAsync(request);

				if (response.Success)
				{
					return Ok(response);
				}
				else
				{
					return BadRequest(response);
				}
			}
			catch (ArgumentException ex)
			{
				return BadRequest(ex.Message);
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(ex.Message);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Internal server error" });
			}
		}

		/// <summary>
		/// Replace all existing routes with new ones using bulk operations
		/// (Delete all existing routes first, then create new ones in bulk)
		/// </summary>
		/// <param name="request">Replace all routes request</param>
		/// <returns>Replace operation result</returns>
		[HttpPost("replace-all")]
		public async Task<ActionResult<ReplaceAllRoutesResponse>> ReplaceAllRoutes([FromBody] ReplaceAllRoutesRequest request)
		{
			try
			{
				if (!ModelState.IsValid)
					return BadRequest(ModelState);

				var response = await _routeService.ReplaceAllRoutesAsync(request);

				if (response.Success)
				{
					return Ok(response);
				}
				else
				{
					return BadRequest(Problem(title: "Replace all routes failed", detail: response.ErrorMessage));
				}
			}
			catch (ArgumentException ex)
			{
				return BadRequest(Problem(title: "Invalid request data", detail: ex.Message));
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Internal server error", detail = ex.Message });
			}
		}

		[HttpDelete("{id:guid}")]
        public async Task<ActionResult> DeleteRoute([FromRoute] Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                    return BadRequest("Invalid route ID");

                var success = await _routeService.SoftDeleteRouteAsync(id);
                if (!success)
                    return NotFound("Route not found");

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPatch("{id:guid}/activate")]
        public async Task<ActionResult> ActivateRoute([FromRoute] Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                    return BadRequest("Invalid route ID");

                var success = await _routeService.ActivateRouteAsync(id);
                if (!success)
                    return NotFound("Route not found");

                return Ok(new { message = "Route activated successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPatch("{id:guid}/deactivate")]
        public async Task<ActionResult> DeactivateRoute([FromRoute] Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                    return BadRequest("Invalid route ID");

                var success = await _routeService.DeactivateRouteAsync(id);
                if (!success)
                    return NotFound("Route not found");

                return Ok(new { message = "Route deactivated successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        #endregion
    }
}