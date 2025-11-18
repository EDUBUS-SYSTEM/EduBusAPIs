using Data.Models;
using Data.Models.Enums;
using System;
using System.Collections.Generic;

namespace Data.Repos.Interfaces
{
    public interface IStudentAbsenceRequestRepository : IMongoRepository<StudentAbsenceRequest>
    {
        Task<(IEnumerable<StudentAbsenceRequest> Items, int TotalCount)> GetByStudentAsync(
            Guid studentId,
            DateTime? startDate,
            DateTime? endDate,
            AbsenceRequestStatus? status,
            CreateAtSortOption sort,
            int page,
            int perPage);

        Task<(IEnumerable<StudentAbsenceRequest> Items, int TotalCount)> GetByParentAsync(
            Guid parentId,
            DateTime? startDate,
            DateTime? endDate,
            AbsenceRequestStatus? status,
            CreateAtSortOption sort,
            int page,
            int perPage);
        Task<(IEnumerable<StudentAbsenceRequest> Items, int TotalCount)> GetAllAsync(
            DateTime? startDate,
            DateTime? endDate,
            AbsenceRequestStatus? status,
            string? studentName,
            CreateAtSortOption sort,
            int page,
            int perPage);
        Task<StudentAbsenceRequest?> GetPendingOverlapAsync(Guid studentId, DateTime startDate, DateTime endDate);
        Task<bool> HasApprovedRequestWithExactRangeAsync(Guid studentId, DateTime startDate, DateTime endDate);
    }
}