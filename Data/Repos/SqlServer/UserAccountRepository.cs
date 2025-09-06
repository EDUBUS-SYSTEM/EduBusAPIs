using Data.Contexts.SqlServer;
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
    public class UserAccountRepository : SqlRepository<UserAccount>, IUserAccountRepository
    {
        public UserAccountRepository(DbContext dbContext) : base(dbContext)
        {
        }

        public async Task<UserAccount?> GetByEmailAsync(string email)
        {
            return await _table
                .Where(u => !u.IsDeleted && u.Email == email)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> IsEmailExistAsync(string email)
        {
            return await _table
                .AnyAsync(u => u.Email.ToLower() == email.ToLower() && !u.IsDeleted);
        }

        public async Task<bool> IsPhoneNumberExistAsync(string phoneNumber)
        {
            return await _table
                  .AnyAsync(u => u.PhoneNumber == phoneNumber);
        }

		// Lock/Unlock users
		public async Task<int> LockUserAsync(Guid userId, DateTime? lockedUntil, string? reason, Guid lockedBy)
		{
			return await _table
				.Where(u => u.Id == userId && !u.IsDeleted)
				.ExecuteUpdateAsync(setters => setters
					.SetProperty(u => u.LockedUntil, lockedUntil)
					.SetProperty(u => u.LockReason, reason)
					.SetProperty(u => u.LockedAt, DateTime.UtcNow)
					.SetProperty(u => u.LockedBy, lockedBy)
					.SetProperty(u => u.UpdatedAt, DateTime.UtcNow));
		}

		public async Task<int> UnlockUserAsync(Guid userId, Guid unlockedBy)
		{
			return await _table
				.Where(u => u.Id == userId && !u.IsDeleted)
				.ExecuteUpdateAsync(setters => setters
					.SetProperty(u => u.LockedUntil, (DateTime?)null)
					.SetProperty(u => u.LockReason, (string?)null)
					.SetProperty(u => u.LockedAt, (DateTime?)null)
					.SetProperty(u => u.LockedBy, (Guid?)null)
					.SetProperty(u => u.UpdatedAt, DateTime.UtcNow));
		}

		public async Task<int> LockUsersAsync(List<Guid> userIds, DateTime? lockedUntil, string? reason, Guid lockedBy)
		{
			if (!userIds.Any()) return 0;

			return await _table
				.Where(u => userIds.Contains(u.Id) && !u.IsDeleted)
				.ExecuteUpdateAsync(setters => setters
					.SetProperty(u => u.LockedUntil, lockedUntil)
					.SetProperty(u => u.LockReason, reason)
					.SetProperty(u => u.LockedAt, DateTime.UtcNow)
					.SetProperty(u => u.LockedBy, lockedBy)
					.SetProperty(u => u.UpdatedAt, DateTime.UtcNow));
		}

		public async Task<int> UnlockUsersAsync(List<Guid> userIds, Guid unlockedBy)
		{
			if (!userIds.Any()) return 0;

			return await _table
				.Where(u => userIds.Contains(u.Id) && !u.IsDeleted)
				.ExecuteUpdateAsync(setters => setters
					.SetProperty(u => u.LockedUntil, (DateTime?)null)
					.SetProperty(u => u.LockReason, (string?)null)
					.SetProperty(u => u.LockedAt, (DateTime?)null)
					.SetProperty(u => u.LockedBy, (Guid?)null)
					.SetProperty(u => u.UpdatedAt, DateTime.UtcNow));
		}

		public async Task<IEnumerable<Admin>> GetAdminUsersAsync()
		{
			return await _table
				.OfType<Admin>()
				.Where(a => !a.IsDeleted)
				.ToListAsync();
		}
	}
}
