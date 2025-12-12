using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Data.Repos.Interfaces;
using Data.Models;
using System.Security.Claims;

namespace APIs.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DeviceTokenController : ControllerBase
    {
        private readonly IDeviceTokenRepository _deviceTokenRepository;
        private readonly ILogger<DeviceTokenController> _logger;

        public DeviceTokenController(
            IDeviceTokenRepository deviceTokenRepository,
            ILogger<DeviceTokenController> logger)
        {
            _deviceTokenRepository = deviceTokenRepository;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterDeviceToken([FromBody] RegisterDeviceTokenDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized("Invalid user token");
                }

                // Check if token already exists
                var existingToken = await _deviceTokenRepository.GetByTokenAsync(dto.Token);
                
                if (existingToken != null)
                {
                    // Update existing token
                    if (existingToken.UserId != userId)
                    {
                        existingToken.UserId = userId;
                    }
                    existingToken.LastUsedAt = DateTime.UtcNow;
                    existingToken.IsActive = true;
                    existingToken.UpdatedAt = DateTime.UtcNow;
                    if (!string.IsNullOrEmpty(dto.Platform))
                    {
                        existingToken.Platform = dto.Platform;
                    }
                    await _deviceTokenRepository.UpdateAsync(existingToken);
                    
                    _logger.LogInformation("Updated existing device token for user {UserId}", userId);
                }
                else
                {
                    // Create new token
                    var deviceToken = new DeviceToken
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Token = dto.Token,
                        Platform = dto.Platform ?? "unknown",
                        CreatedAt = DateTime.UtcNow,
                        LastUsedAt = DateTime.UtcNow,
                        IsActive = true
                    };
                    await _deviceTokenRepository.AddAsync(deviceToken);
                    
                    _logger.LogInformation("Registered new device token for user {UserId}", userId);
                }

                return Ok(new { message = "Device token registered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering device token");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("unregister")]
        public async Task<IActionResult> UnregisterDeviceToken([FromBody] UnregisterDeviceTokenDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized("Invalid user token");
                }

                await _deviceTokenRepository.DeactivateTokenAsync(dto.Token);
                _logger.LogInformation("Unregistered device token for user {UserId}", userId);
                
                return Ok(new { message = "Device token unregistered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unregistering device token");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }

    public class RegisterDeviceTokenDto
    {
        public string Token { get; set; } = string.Empty;
        public string? Platform { get; set; }
    }

    public class UnregisterDeviceTokenDto
    {
        public string Token { get; set; } = string.Empty;
    }
}

