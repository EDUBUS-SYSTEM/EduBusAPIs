using Data.Models;
using Services.Models.UserAccount;

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