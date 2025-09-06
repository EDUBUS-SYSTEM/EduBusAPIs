using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.Configuration;
using Utils;

namespace APIs.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class ConfigurationController : ControllerBase
    {
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<ConfigurationController> _logger;

        public ConfigurationController(
            IConfigurationService configurationService,
            ILogger<ConfigurationController> logger)
        {
            _configurationService = configurationService;
            _logger = logger;
        }

        [HttpGet("leave-request-settings")]
        public ActionResult<LeaveRequestSettings> GetLeaveRequestSettings()
        {
            try
            {
                var settings = _configurationService.GetLeaveRequestSettings();
                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving leave request settings");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("leave-request-settings")]
        public async Task<ActionResult<LeaveRequestSettings>> UpdateLeaveRequestSettings([FromBody] LeaveRequestSettings settings)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Validate settings
                if (settings.MinimumAdvanceNoticeHours < 0)
                {
                    return BadRequest("Minimum advance notice hours cannot be negative.");
                }

                if (settings.EmergencyLeaveAdvanceNoticeHours < 0)
                {
                    return BadRequest("Emergency leave advance notice hours cannot be negative.");
                }

                if (settings.EmergencyLeaveAdvanceNoticeHours > settings.MinimumAdvanceNoticeHours)
                {
                    return BadRequest("Emergency leave advance notice hours cannot be greater than minimum advance notice hours.");
                }

                var updatedSettings = await _configurationService.UpdateLeaveRequestSettingsAsync(settings);
                _logger.LogInformation("Leave request settings updated by admin {AdminId}. " +
                    "MinimumAdvanceNoticeHours: {MinHours}, EmergencyAdvanceNoticeHours: {EmergencyHours}, " +
                    "AllowEmergencyLeaveRequests: {AllowEmergency}", 
                    AuthorizationHelper.GetCurrentUserId(HttpContext),
                    updatedSettings.MinimumAdvanceNoticeHours,
                    updatedSettings.EmergencyLeaveAdvanceNoticeHours,
                    updatedSettings.AllowEmergencyLeaveRequests);
                
                return Ok(updatedSettings);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid leave request settings provided");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating leave request settings");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
