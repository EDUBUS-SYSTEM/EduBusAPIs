using Data.Models;

namespace Data.Repos.Interfaces
{
    public interface IParentRegistrationRepository : IMongoRepository<ParentRegistrationDocument>
    {
        Task<ParentRegistrationDocument?> FindByEmailAsync(string email);
        Task<ParentRegistrationDocument?> FindByEmailAnyStatusAsync(string email);
        Task<List<ParentRegistrationDocument>> GetExpiredRegistrationsAsync();
        Task CleanupExpiredRegistrationsAsync();
    }
}
