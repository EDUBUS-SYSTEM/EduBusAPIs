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
    public class UserRepository : SqlRepository<UserAccount>, IUserRepository
    {
        public UserRepository(DbContext dbContext) : base(dbContext)
        {
        }
        public async Task<bool> IsEmailExistAsync(string email)
        {
            return await _table
                .AnyAsync(u => u.Email.ToLower() == email.ToLower() && !u.IsDeleted);
        }

        public async Task<UserAccount?> FindByEmailAsync(string email)
        {
            return await _table
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && !u.IsDeleted);
        }
    }
}
