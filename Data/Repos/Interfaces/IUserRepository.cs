using Data.Models;

namespace Data.Repos.Interfaces
{
    public interface IUserRepository:ISqlRepository<UserAccount>
    {
        Task<bool> IsEmailExistAsync(string email);
        Task<UserAccount?> FindByEmailAsync(string email);
    }
}
