using Data.Models;

namespace Data.Repos.Interfaces
{
    public interface IStudentAbsenceRequestRepository : IMongoRepository<StudentAbsenceRequest>
    {
        Task<IEnumerable<StudentAbsenceRequest>> GetByStudentAsync(Guid studentId);
        Task<IEnumerable<StudentAbsenceRequest>> GetByParentAsync(Guid parentId);
        Task<StudentAbsenceRequest?> GetPendingOverlapAsync(Guid studentId, DateTime startDate, DateTime endDate);
        Task<bool> HasApprovedRequestWithExactRangeAsync(Guid studentId, DateTime startDate, DateTime endDate);
    }
}