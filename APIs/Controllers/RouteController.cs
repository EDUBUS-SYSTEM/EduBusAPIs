using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.Route;
using System.ComponentModel.DataAnnotations;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoutesController : ControllerBase
    {
        private readonly IRouteSuggestionService _routeSuggestionService;
        private readonly ILogger<RoutesController> _logger;

        public RoutesController(
            IRouteSuggestionService routeSuggestionService,
            ILogger<RoutesController> logger)
        {
            _routeSuggestionService = routeSuggestionService;
            _logger = logger;
        }

        [HttpPost("suggestions")]
        public async Task<ActionResult<RouteSuggestionResponse>> GenerateRouteSuggestions(
            [FromBody] RouteSuggestionRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Validate latitude/longitude if provided
                if (request.SchoolLatitude.HasValue &&
                    (request.SchoolLatitude < -90 || request.SchoolLatitude > 90))
                {
                    return BadRequest("Invalid latitude. Must be between -90 and 90.");
                }

                if (request.SchoolLongitude.HasValue &&
                    (request.SchoolLongitude < -180 || request.SchoolLongitude > 180))
                {
                    return BadRequest("Invalid longitude. Must be between -180 and 180.");
                }

                var response = await _routeSuggestionService.GenerateRouteSuggestionsAsync(request);

                return response.Success ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating route suggestions");
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
                _logger.LogError(ex, "Error optimizing route {RouteId}", routeId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}