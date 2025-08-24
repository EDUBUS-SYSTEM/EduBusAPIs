using AutoMapper;
using Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.UserAccount;
using System.Security.Claims;

namespace APIs.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService) => _authService = authService;

        // POST /auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest dto)
        {
            var user = await _authService.LoginAsync(dto);
            if (user == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    data = (object?)null,
                    error = new { code = "INVALID_CREDENTIALS", message = "Email or password is incorrect" }
                });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    accessToken = user.Token,
                    refreshToken = user.RefreshToken,
                    fullName = user.FullName,
                    role = user.Role,
                    expiresAtUtc = user.ExpiresAtUtc
                },
                error = (object?)null
            });
        }

        // POST /auth/logout
        [Authorize]
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
            {
                _authService.InvalidateRefreshToken(Guid.Parse(userId));
            }

            return Ok(new
            {
                success = true,
                data = new { message = "Logout successful" },
                error = (object?)null
            });
        }

        // POST /auth/refresh-token
        [HttpPost("refresh-token")]
        public IActionResult Refresh([FromBody] RefreshTokenRequest dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr))
            {
                return Unauthorized(new
                {
                    success = false,
                    data = (object?)null,
                    error = new { code = "UNAUTHORIZED", message = "Missing user id" }
                });
            }

            var userId = Guid.Parse(userIdStr);
            if (!_authService.ValidateRefreshToken(userId, dto.RefreshToken))
            {
                return Unauthorized(new
                {
                    success = false,
                    data = (object?)null,
                    error = new { code = "INVALID_REFRESH_TOKEN", message = "Refresh token invalid or expired" }
                });
            }

            // regenerate tokens
            var user = _authService.GetUserById(userId);
            if (user == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    data = (object?)null,
                    error = new { code = "USER_NOT_FOUND", message = "User not found" }
                });
            }

            var role = user switch
            {
                Admin => "Admin",
                Driver => "Driver",
                Parent => "Parent",
                _ => "Unknown"
            };

            var (newAccess, newRefresh, expires) = _authService.GenerateTokens(user, role);

            return Ok(new
            {
                success = true,
                data = new
                {
                    accessToken = newAccess,
                    refreshToken = newRefresh,
                    expiresAtUtc = expires
                },
                error = (object?)null
            });
        }
    }
}
