using Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.UserAccount;
using System.Security.Claims;
using Constants;

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
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
            {
                await _authService.LogoutAsync(Guid.Parse(userId));
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
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest dto)
        {
            var result = await _authService.RefreshTokensAsync(dto.RefreshToken);
            if (result == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    data = (object?)null,
                    error = new { code = "INVALID_REFRESH_TOKEN", message = "Refresh token invalid or expired" }
                });
            }

            var (newAccess, newRefresh, expires) = result.Value;

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

        //API - Test Author
        // GET /auth/admin
        [Authorize(Roles = Roles.Admin)]
        [HttpGet("admin")]
        public IActionResult AdminOnly()
        {
            return Ok(new
            {
                success = true,
                data = new { message = "You are an Admin!" },
                error = (object?)null
            });
        }

        // GET /auth/driver
        [Authorize(Roles = Roles.Driver)]
        [HttpGet("driver")]
        public IActionResult DriverOnly()
        {
            return Ok(new
            {
                success = true,
                data = new { message = "You are a Driver!" },
                error = (object?)null
            });
        }

        // GET /auth/parent
        [Authorize(Roles = Roles.Parent)]
        [HttpGet("parent")]
        public IActionResult ParentOnly()
        {
            return Ok(new
            {
                success = true,
                data = new { message = "You are a Parent!" },
                error = (object?)null
            });
        }

        // GET /auth/any
        [Authorize(Roles = Roles.AllRoles)]
        [HttpGet("any")]
        public IActionResult AnyRole()
        {
            return Ok(new
            {
                success = true,
                data = new { message = "You are authenticated with a valid role!" },
                error = (object?)null
            });
        }
    }
}