using Data.Models;
using Data.Repos.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repos.SqlServer
{
    public class DeviceTokenRepository : SqlRepository<DeviceToken>, IDeviceTokenRepository
    {
        public DeviceTokenRepository(DbContext dbContext) : base(dbContext)
        {
        }

        public async Task<DeviceToken?> GetByTokenAsync(string token)
        {
            return await _dbContext.Set<DeviceToken>()
                .FirstOrDefaultAsync(x => x.Token == token && x.IsActive && !x.IsDeleted);
        }

        public async Task<IEnumerable<DeviceToken>> GetByUserIdAsync(Guid userId)
        {
            return await _dbContext.Set<DeviceToken>()
                .Where(x => x.UserId == userId && x.IsActive && !x.IsDeleted)
                .ToListAsync();
        }

        public async Task DeactivateTokenAsync(string token)
        {
            var deviceToken = await GetByTokenAsync(token);
            if (deviceToken != null)
            {
                deviceToken.IsActive = false;
                deviceToken.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task DeactivateAllUserTokensAsync(Guid userId)
        {
            var tokens = await _dbContext.Set<DeviceToken>()
                .Where(x => x.UserId == userId && x.IsActive && !x.IsDeleted)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.IsActive = false;
                token.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}
