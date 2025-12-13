using Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repos.Interfaces
{
    public interface IDeviceTokenRepository : ISqlRepository<DeviceToken>
    {
        Task<DeviceToken?> GetByTokenAsync(string token);
        Task<IEnumerable<DeviceToken>> GetByUserIdAsync(Guid userId);
        Task DeactivateTokenAsync(string token);
        Task DeactivateAllUserTokensAsync(Guid userId);
    }
}
