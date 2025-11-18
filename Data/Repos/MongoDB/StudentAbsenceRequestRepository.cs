using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repos.MongoDB
{
    public class StudentAbsenceRequestRepository : MongoRepository<StudentAbsenceRequest>, IStudentAbsenceRequestRepository
    {
        public StudentAbsenceRequestRepository(IMongoDatabase database)
            : base(database, "student_absence_requests")
        {
        }

        public Task<IEnumerable<StudentAbsenceRequest>> GetByStudentAsync(Guid studentId)
        {
            var filter = Builders<StudentAbsenceRequest>.Filter.Eq(x => x.StudentId, studentId);
            var sort = Builders<StudentAbsenceRequest>.Sort.Descending(x => x.CreatedAt);
            return FindByFilterAsync(filter, sort);
        }

        public Task<IEnumerable<StudentAbsenceRequest>> GetByParentAsync(Guid parentId)
        {
            var filter = Builders<StudentAbsenceRequest>.Filter.Eq(x => x.ParentId, parentId);
            var sort = Builders<StudentAbsenceRequest>.Sort.Descending(x => x.CreatedAt);
            return FindByFilterAsync(filter, sort);
        }

        public async Task<StudentAbsenceRequest?> GetPendingOverlapAsync(
            Guid studentId,
            DateTime startDate,
            DateTime endDate)
        {
            var filter = Builders<StudentAbsenceRequest>.Filter.And(
                Builders<StudentAbsenceRequest>.Filter.Eq(x => x.StudentId, studentId),
                Builders<StudentAbsenceRequest>.Filter.Eq(x => x.Status, AbsenceRequestStatus.Pending),
                Builders<StudentAbsenceRequest>.Filter.Gte(x => x.EndDate, startDate),
                Builders<StudentAbsenceRequest>.Filter.Lte(x => x.StartDate, endDate),
                Builders<StudentAbsenceRequest>.Filter.Eq(x => x.IsDeleted, false)
            );

            return await _collection
                .Find(filter)
                .SortByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> HasApprovedRequestWithExactRangeAsync(
            Guid studentId,
            DateTime startDate,
            DateTime endDate)
        {
            var filter = Builders<StudentAbsenceRequest>.Filter.And(
                Builders<StudentAbsenceRequest>.Filter.Eq(x => x.StudentId, studentId),
                Builders<StudentAbsenceRequest>.Filter.Eq(x => x.Status, AbsenceRequestStatus.Approved),
                Builders<StudentAbsenceRequest>.Filter.Eq(x => x.StartDate, startDate),
                Builders<StudentAbsenceRequest>.Filter.Eq(x => x.EndDate, endDate),
                Builders<StudentAbsenceRequest>.Filter.Eq(x => x.IsDeleted, false)
            );

            return await _collection.Find(filter).AnyAsync();
        }
    }
}
