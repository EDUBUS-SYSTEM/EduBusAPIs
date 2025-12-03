using Data.Models;
using Services.Models.UserAccount;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Contracts
{
    public interface IAuthService
    {
        Task<AuthResponse?> LoginAsync(LoginRequest request);
        Task LogoutAsync(Guid userId);
        Task<(string accessToken, string refreshToken, DateTime expiresUtc)?> RefreshTokensAsync(string refreshToken);
        Task<bool> SendOtpAsync(string email);
        Task<bool> VerifyOtpAndResetPasswordAsync(string email, string otpCode, string newPassword);
        Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
    }
}