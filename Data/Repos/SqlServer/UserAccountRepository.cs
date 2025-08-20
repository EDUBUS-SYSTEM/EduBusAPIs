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
        private readonly EduBusSqlContext _context;
        public UserAccountRepository(EduBusSqlContext context) : base(context) => _context = context;

        public async Task<UserAccount?> GetByEmailAsync(string email)
        {
            return await _context.UserAccounts
                .Where(u => !u.IsDeleted && u.Email == email)
                .FirstOrDefaultAsync();
        }
    }
}
