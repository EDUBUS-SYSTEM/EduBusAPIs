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
    }
}
