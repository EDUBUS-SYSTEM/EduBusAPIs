using Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repos.Interfaces
{
    public interface IUserAccountRepository : ISqlRepository<UserAccount>
    {
        Task<UserAccount?> GetByEmailAsync(string email);
        Task<bool> IsEmailExistAsync(string email);
        Task<bool> IsPhoneNumberExistAsync(string phoneNumber);
        Task<int> LockUserAsync(Guid userId, DateTime? lockedUntil, string? reason, Guid lockedBy);
        Task<int> UnlockUserAsync(Guid userId, Guid unlockedBy);
        Task<int> LockUsersAsync(List<Guid> userIds, DateTime? lockedUntil, string? reason, Guid lockedBy);
		Task<int> UnlockUsersAsync(List<Guid> userIds, Guid unlockedBy);
		Task<IEnumerable<Admin>> GetAdminUsersAsync();
	}
}
