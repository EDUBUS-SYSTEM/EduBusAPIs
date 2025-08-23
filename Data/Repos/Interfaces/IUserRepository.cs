using Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repos.Interfaces
{
    public interface IUserRepository:ISqlRepository<UserAccount>
    {
        Task<bool> IsEmailExistAsync(string email);
        Task<UserAccount?> FindByEmailAsync(string email);
    }
}
