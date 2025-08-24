using Data.Contexts.SqlServer;
using Data.Models;
using Data.Repos.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Data.Repos.SqlServer
{
    public class RefreshTokenRepository : SqlRepository<RefreshToken>, IRefreshTokenRepository
    {
        private readonly EduBusSqlContext _context;

        public RefreshTokenRepository(EduBusSqlContext context) : base(context)
        {
            _context = context;
        }

        public async Task<RefreshToken?> GetByTokenAsync(string token)
        {
            return await _context.Set<RefreshToken>()
                .FirstOrDefaultAsync(x => x.Token == token && !x.IsDeleted);
        }

        public async Task InvalidateUserTokensAsync(Guid userId)
        {
            var tokens = await _context.RefreshTokens
                .Where(r => r.UserId == userId && r.RevokedAtUtc == null && r.ExpiresAtUtc > DateTime.UtcNow)
                .ToListAsync();

            foreach (var t in tokens)
            {
                t.RevokedAtUtc = DateTime.UtcNow;
                t.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Ensure user only has max N tokens. Delete the oldest ones if exceed.
        /// </summary>
        public async Task EnforceUserTokenLimitAsync(Guid userId, int maxTokens)
        {
            var tokens = await _context.RefreshTokens
                .Where(r => r.UserId == userId && !r.IsDeleted)
                .OrderByDescending(r => r.CreatedAtUtc) 
                .ToListAsync();

            if (tokens.Count > maxTokens)
            {
                var tokensToDelete = tokens.Skip(maxTokens).ToList();
                _context.RefreshTokens.RemoveRange(tokensToDelete);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Cleanup expired or revoked tokens
        /// </summary>
        public async Task CleanupExpiredTokensAsync()
        {
            var expiredTokens = await _context.RefreshTokens
                .Where(r => r.ExpiresAtUtc <= DateTime.UtcNow || r.RevokedAtUtc != null)
                .ToListAsync();

            if (expiredTokens.Any())
            {
                _context.RefreshTokens.RemoveRange(expiredTokens);
                await _context.SaveChangesAsync();
            }
        }
    }
}
