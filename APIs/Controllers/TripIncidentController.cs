using System;
using Constants;
using Data.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.TripIncident;
using Utils;

namespace APIs.Controllers
{
    [ApiController]
    [Route("api/trip-incidents")]
    public class TripIncidentController : ControllerBase
    {
        private readonly ITripIncidentService _incidentService;
        private readonly ILogger<TripIncidentController> _logger;

        public TripIncidentController(
            ITripIncidentService incidentService,
            ILogger<TripIncidentController> logger)
        {
            _incidentService = incidentService;
            _logger = logger;
        }

        [Authorize(Roles = Roles.Supervisor)]
        [HttpPost("trips/{tripId:guid}")]
        public async Task<IActionResult> Create(Guid tripId, [FromBody] CreateTripIncidentRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var supervisorId = AuthorizationHelper.GetCurrentUserId(HttpContext);
            if (!supervisorId.HasValue)
                return Unauthorized(new { message = "Supervisor ID not found." });

            try
            {
                var created = await _incidentService.CreateAsync(tripId, request, supervisorId.Value);
                return CreatedAtAction(nameof(GetById), new { incidentId = created.Id }, created);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating trip incident");
                return StatusCode(500, new { message = "An error occurred while creating incident report." });
            }
        }

        [Authorize(Roles = $"{Roles.Admin},{Roles.Supervisor}")]
        [HttpGet("trips/{tripId:guid}")]
        public async Task<IActionResult> GetByTrip(
            Guid tripId,
            [FromQuery] int page = 1,
            [FromQuery] int perPage = 20)
        {
            var requesterId = AuthorizationHelper.GetCurrentUserId(HttpContext);
            if (!requesterId.HasValue)
                return Unauthorized(new { message = "User ID not found." });

            var isAdmin = User.IsInRole(Roles.Admin);

            try
            {
                var response = await _incidentService.GetByTripAsync(tripId, requesterId.Value, isAdmin, page, perPage);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = $"{Roles.Admin},{Roles.Supervisor}")]
        [HttpGet("{incidentId:guid}")]
        public async Task<IActionResult> GetById(Guid incidentId)
        {
            var requesterId = AuthorizationHelper.GetCurrentUserId(HttpContext);
            if (!requesterId.HasValue)
                return Unauthorized(new { message = "User ID not found." });

            var isAdmin = User.IsInRole(Roles.Admin);

            try
            {
                var incident = await _incidentService.GetByIdAsync(incidentId, requesterId.Value, isAdmin);
                if (incident is null)
                    return NotFound(new { message = "Incident not found." });

                return Ok(incident);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] Guid? tripId,
            [FromQuery] Guid? supervisorId,
            [FromQuery] TripIncidentStatus? status,
            [FromQuery] int page = 1,
            [FromQuery] int perPage = 20)
        {
            try
            {
                var response = await _incidentService.GetAllAsync(tripId, supervisorId, status, page, perPage);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting incident reports");
                return StatusCode(500, new { message = "An error occurred while getting incident reports." });
            }
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpPatch("{incidentId:guid}/status")]
        public async Task<IActionResult> UpdateStatus(Guid incidentId, [FromBody] UpdateTripIncidentStatusDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var adminId = AuthorizationHelper.GetCurrentUserId(HttpContext);
            if (!adminId.HasValue)
                return Unauthorized(new { message = "Admin ID not found." });

            try
            {
                var updated = await _incidentService.UpdateStatusAsync(incidentId, request, adminId.Value);
                return Ok(updated);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating incident status");
                return StatusCode(500, new { message = "An error occurred while updating incident status." });
            }
        }
    }
}

