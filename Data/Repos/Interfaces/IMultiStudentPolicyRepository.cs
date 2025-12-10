using Data.Models;

namespace Data.Repos.Interfaces
{
    public interface IMultiStudentPolicyRepository : IMongoRepository<MultiStudentPolicyDocument>
    {
        /// <summary>
        /// Get all active and effective policies
        /// </summary>
        Task<List<MultiStudentPolicyDocument>> GetActivePoliciesAsync();

        /// <summary>
        /// Get current active and effective policy
        /// </summary>
        Task<MultiStudentPolicyDocument?> GetCurrentActivePolicyAsync();

        /// <summary>
        /// Get effective policy for a specific date
        /// </summary>
        Task<MultiStudentPolicyDocument?> GetEffectivePolicyAsync(DateTime date);

        /// <summary>
        /// Get all policies (including deleted)
        /// </summary>
        Task<List<MultiStudentPolicyDocument>> GetAllIncludingDeletedAsync();
    }
}
