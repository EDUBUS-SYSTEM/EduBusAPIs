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
        Task<UserAccount?> GetUserByIdAsync(Guid userId);
    }
}