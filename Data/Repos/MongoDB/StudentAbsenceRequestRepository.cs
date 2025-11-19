using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Data.Repos.MongoDB
{
    public class StudentAbsenceRequestRepository : MongoRepository<StudentAbsenceRequest>, IStudentAbsenceRequestRepository
    {
        public StudentAbsenceRequestRepository(IMongoDatabase database)
            : base(database, "student_absence_requests")
        {
        }

        public async Task<(IEnumerable<StudentAbsenceRequest> Items, int TotalCount)> GetByStudentAsync(
            Guid studentId,
            DateTime? startDate,
            DateTime? endDate,
            AbsenceRequestStatus? status,
            CreateAtSortOption sort,
            int page,
            int perPage)
        {
            var filterBuilder = Builders<StudentAbsenceRequest>.Filter;
            var filters = new List<FilterDefinition<StudentAbsenceRequest>>
            {
                filterBuilder.Eq(x => x.StudentId, studentId),
                filterBuilder.Eq(x => x.IsDeleted, false)
            };

            if (startDate.HasValue)
            {
                filters.Add(filterBuilder.Gte(x => x.StartDate, startDate.Value));
            }

            if (endDate.HasValue)
            {
                filters.Add(filterBuilder.Lte(x => x.EndDate, endDate.Value));
            }

            if (status.HasValue)
            {
                filters.Add(filterBuilder.Eq(x => x.Status, status.Value));
            }

            var filter = filterBuilder.And(filters);
            var sortDefinition = sort == CreateAtSortOption.Oldest
                ? Builders<StudentAbsenceRequest>.Sort.Ascending(x => x.CreatedAt)
                : Builders<StudentAbsenceRequest>.Sort.Descending(x => x.CreatedAt);

            var skip = Math.Max(0, (page - 1) * perPage);

            var totalCount = (int)await _collection.CountDocumentsAsync(filter);
            var items = await _collection
                .Find(filter)
                .Sort(sortDefinition)
                .Skip(skip)
                .Limit(perPage)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(IEnumerable<StudentAbsenceRequest> Items, int TotalCount)> GetByParentAsync(
            Guid parentId,
            DateTime? startDate,
            DateTime? endDate,
            AbsenceRequestStatus? status,
            CreateAtSortOption sort,
            int page,
            int perPage)
        {
            var filterBuilder = Builders<StudentAbsenceRequest>.Filter;
            var filters = new List<FilterDefinition<StudentAbsenceRequest>>
            {
                filterBuilder.Eq(x => x.ParentId, parentId),
                filterBuilder.Eq(x => x.IsDeleted, false)
            };

            if (startDate.HasValue)
            {
                filters.Add(filterBuilder.Gte(x => x.StartDate, startDate.Value));
            }

            if (endDate.HasValue)
            {
                filters.Add(filterBuilder.Lte(x => x.EndDate, endDate.Value));
        }

            if (status.HasValue)
            {
                filters.Add(filterBuilder.Eq(x => x.Status, status.Value));
            }

            var filter = filterBuilder.And(filters);
            var sortDefinition = sort == CreateAtSortOption.Oldest
                ? Builders<StudentAbsenceRequest>.Sort.Ascending(x => x.CreatedAt)
                : Builders<StudentAbsenceRequest>.Sort.Descending(x => x.CreatedAt);

            var skip = Math.Max(0, (page - 1) * perPage);

            var totalCount = (int)await _collection.CountDocumentsAsync(filter);
            var items = await _collection
                .Find(filter)
                .Sort(sortDefinition)
                .Skip(skip)
                .Limit(perPage)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(IEnumerable<StudentAbsenceRequest> Items, int TotalCount)> GetAllAsync(
            DateTime? startDate,
            DateTime? endDate,
            AbsenceRequestStatus? status,
            string? studentName,
            CreateAtSortOption sort,
            int page,
            int perPage)
        {
            var filterBuilder = Builders<StudentAbsenceRequest>.Filter;
            var filters = new List<FilterDefinition<StudentAbsenceRequest>>
            {
                filterBuilder.Eq(x => x.IsDeleted, false)
            };

            if (startDate.HasValue)
            {
                filters.Add(filterBuilder.Gte(x => x.StartDate, startDate.Value));
            }

            if (endDate.HasValue)
            {
                filters.Add(filterBuilder.Lte(x => x.EndDate, endDate.Value));
            }

            if (status.HasValue)
        {
                filters.Add(filterBuilder.Eq(x => x.Status, status.Value));
        }

            if (!string.IsNullOrWhiteSpace(studentName))
        {
                var escaped = Regex.Escape(studentName.Trim());
                var regex = new BsonRegularExpression(escaped, "i");
                filters.Add(filterBuilder.Regex(x => x.StudentName, regex));
            }

            var filter = filterBuilder.And(filters);
            var sortDefinition = sort == CreateAtSortOption.Oldest
                ? Builders<StudentAbsenceRequest>.Sort.Ascending(x => x.CreatedAt)
                : Builders<StudentAbsenceRequest>.Sort.Descending(x => x.CreatedAt);

            var skip = Math.Max(0, (page - 1) * perPage);

            var totalCount = (int)await _collection.CountDocumentsAsync(filter);
            var items = await _collection
                .Find(filter)
                .Sort(sortDefinition)
                .Skip(skip)
                .Limit(perPage)
                .ToListAsync();

            return (items, totalCount);
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
