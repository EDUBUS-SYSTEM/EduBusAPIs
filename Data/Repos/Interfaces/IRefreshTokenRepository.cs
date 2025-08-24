using Data.Models;

namespace Data.Repos.Interfaces
{
    public interface IRefreshTokenRepository : ISqlRepository<RefreshToken>
    {
        Task<RefreshToken?> GetByTokenAsync(string token);
        Task InvalidateUserTokensAsync(Guid userId);
        Task EnforceUserTokenLimitAsync(Guid userId, int maxTokens);
        Task CleanupExpiredTokensAsync();
    }
}
