using Data.Models;
using Data.Repos.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.UserAccount;
using System.Security.Claims;
using Constants;
using Utils;

namespace APIs.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IUserAccountRepository _userAccountRepository;
        
        public AuthController(IAuthService authService, IUserAccountRepository userAccountRepository)
        {
            _authService = authService;
            _userAccountRepository = userAccountRepository;
        }

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

        // POST /auth/send-otp
        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
        {
            var result = await _authService.SendOtpAsync(request.Email);
            if (!result)
            {
                return NotFound(new
                {
                    success = false,
                    data = (object?)null,
                    error = new { code = "USER_NOT_FOUND", message = "No user found with this email" }
                });
            }

            return Ok(new
            {
                success = true,
                data = new { message = "OTP sent successfully to your email" },
                error = (object?)null
            });
        }

        // POST /auth/verify-otp-reset
        [HttpPost("verify-otp-reset")]
        public async Task<IActionResult> VerifyOtpReset([FromBody] PasswordResetOtpRequest request)
        {
            try
            {
                var result = await _authService.VerifyOtpAndResetPasswordAsync(request.Email, request.OtpCode, request.NewPassword);
                if (!result)
                {
                    return BadRequest(new
                    {
                        success = false,
                        data = (object?)null,
                        error = new { code = "INVALID_OTP", message = "Invalid or expired OTP code" }
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = new { message = "Password reset successfully" },
                    error = (object?)null
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    data = (object?)null,
                    error = new { code = "VALIDATION_ERROR", message = ex.Message }
                });
            }
        }

        // POST /auth/change-password
        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                await _authService.ChangePasswordAsync(Guid.Parse(userId), request.CurrentPassword, request.NewPassword);

                return Ok(new
                {
                    success = true,
                    data = new { message = "Password changed successfully" },
                    error = (object?)null
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    data = (object?)null,
                    error = new { code = "VALIDATION_ERROR", message = ex.Message }
                });
            }
        }

    }
}