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
        void InvalidateRefreshToken(Guid userId);
        bool ValidateRefreshToken(Guid userId, string refreshToken);
        UserAccount? GetUserById(Guid userId);
        (string accessToken, string refreshToken, DateTime expiresUtc) GenerateTokens(UserAccount user, string role);
    }
}
