using Data.Models;
using Data.Repos.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Data.Repos.MongoDB
{
    public class EnrollmentSemesterSettingsRepository : MongoRepository<EnrollmentSemesterSettings>, IEnrollmentSemesterSettingsRepository
    {
        public EnrollmentSemesterSettingsRepository(IMongoDatabase database)
            : base(database, "EnrollmentSemesterSettings")
        {
        }

        public async Task<EnrollmentSemesterSettings?> GetActiveSettingsAsync()
        {
            var filter = Builders<EnrollmentSemesterSettings>.Filter.And(
                Builders<EnrollmentSemesterSettings>.Filter.Eq(x => x.IsActive, true),
                Builders<EnrollmentSemesterSettings>.Filter.Eq(x => x.IsDeleted, false)
            );
            var sort = Builders<EnrollmentSemesterSettings>.Sort.Descending(x => x.CreatedAt);
            return await _collection.Find(filter).Sort(sort).FirstOrDefaultAsync();
        }

        public async Task<EnrollmentSemesterSettings?> FindBySemesterCodeAsync(string semesterCode)
        {
            var filter = Builders<EnrollmentSemesterSettings>.Filter.And(
                Builders<EnrollmentSemesterSettings>.Filter.Eq(x => x.SemesterCode, semesterCode),
                Builders<EnrollmentSemesterSettings>.Filter.Eq(x => x.IsDeleted, false)
            );
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<List<EnrollmentSemesterSettings>> FindByAcademicYearAsync(string academicYear)
        {
            var filter = Builders<EnrollmentSemesterSettings>.Filter.And(
                Builders<EnrollmentSemesterSettings>.Filter.Eq(x => x.AcademicYear, academicYear),
                Builders<EnrollmentSemesterSettings>.Filter.Eq(x => x.IsDeleted, false)
            );
            var sort = Builders<EnrollmentSemesterSettings>.Sort.Descending(x => x.SemesterStartDate);
            return await _collection.Find(filter).Sort(sort).ToListAsync();
        }

        public async Task<EnrollmentSemesterSettings?> GetCurrentOpenRegistrationAsync()
        {
            var now = DateTime.UtcNow;
            // Normalize now to date only (00:00:00) for comparison with normalized dates
            // This ensures registration is open for the entire day
            var today = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
            
            var filter = Builders<EnrollmentSemesterSettings>.Filter.And(
                Builders<EnrollmentSemesterSettings>.Filter.Eq(x => x.IsActive, true),
                Builders<EnrollmentSemesterSettings>.Filter.Eq(x => x.IsDeleted, false),
                // Registration has started (start date <= today)
                Builders<EnrollmentSemesterSettings>.Filter.Lte(x => x.RegistrationStartDate, today),
                // Registration has not ended (end date >= today, meaning it's still open today)
                Builders<EnrollmentSemesterSettings>.Filter.Gte(x => x.RegistrationEndDate, today)
            );
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<List<EnrollmentSemesterSettings>> QueryAsync(
            string? semesterCode = null,
            string? academicYear = null,
            bool? isActive = null,
            string? searchTerm = null,
            int skip = 0,
            int limit = 0,
            string? sortBy = null,
            bool sortDescending = true)
        {
            var filters = new List<FilterDefinition<EnrollmentSemesterSettings>>
            {
                Builders<EnrollmentSemesterSettings>.Filter.Eq(x => x.IsDeleted, false)
            };

            if (!string.IsNullOrWhiteSpace(semesterCode))
            {
                filters.Add(Builders<EnrollmentSemesterSettings>.Filter.Eq(x => x.SemesterCode, semesterCode));
            }

            if (!string.IsNullOrWhiteSpace(academicYear))
            {
                filters.Add(Builders<EnrollmentSemesterSettings>.Filter.Eq(x => x.AcademicYear, academicYear));
            }

            if (isActive.HasValue)
            {
                filters.Add(Builders<EnrollmentSemesterSettings>.Filter.Eq(x => x.IsActive, isActive.Value));
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var searchFilter = Builders<EnrollmentSemesterSettings>.Filter.Or(
                    Builders<EnrollmentSemesterSettings>.Filter.Regex(x => x.SemesterName, new BsonRegularExpression(searchTerm, "i")),
                    Builders<EnrollmentSemesterSettings>.Filter.Regex(x => x.SemesterCode, new BsonRegularExpression(searchTerm, "i")),
                    Builders<EnrollmentSemesterSettings>.Filter.Regex(x => x.AcademicYear, new BsonRegularExpression(searchTerm, "i")),
                    Builders<EnrollmentSemesterSettings>.Filter.Regex(x => x.Description ?? "", new BsonRegularExpression(searchTerm, "i"))
                );
                filters.Add(searchFilter);
            }

            var filter = filters.Count == 1 ? filters[0] : Builders<EnrollmentSemesterSettings>.Filter.And(filters);

            SortDefinition<EnrollmentSemesterSettings> sort = sortBy?.ToLowerInvariant() switch
            {
                "semestername" => sortDescending 
                    ? Builders<EnrollmentSemesterSettings>.Sort.Descending(x => x.SemesterName) 
                    : Builders<EnrollmentSemesterSettings>.Sort.Ascending(x => x.SemesterName),
                "semestercode" => sortDescending 
                    ? Builders<EnrollmentSemesterSettings>.Sort.Descending(x => x.SemesterCode) 
                    : Builders<EnrollmentSemesterSettings>.Sort.Ascending(x => x.SemesterCode),
                "academicyear" => sortDescending 
                    ? Builders<EnrollmentSemesterSettings>.Sort.Descending(x => x.AcademicYear) 
                    : Builders<EnrollmentSemesterSettings>.Sort.Ascending(x => x.AcademicYear),
                "semesterstartdate" => sortDescending 
                    ? Builders<EnrollmentSemesterSettings>.Sort.Descending(x => x.SemesterStartDate) 
                    : Builders<EnrollmentSemesterSettings>.Sort.Ascending(x => x.SemesterStartDate),
                "registrationstartdate" => sortDescending 
                    ? Builders<EnrollmentSemesterSettings>.Sort.Descending(x => x.RegistrationStartDate) 
                    : Builders<EnrollmentSemesterSettings>.Sort.Ascending(x => x.RegistrationStartDate),
                "createdat" or _ => sortDescending 
                    ? Builders<EnrollmentSemesterSettings>.Sort.Descending(x => x.CreatedAt) 
                    : Builders<EnrollmentSemesterSettings>.Sort.Ascending(x => x.CreatedAt)
            };

            var query = _collection.Find(filter).Sort(sort);

            if (skip > 0)
            {
                query = query.Skip(skip);
            }

            if (limit > 0)
            {
                query = query.Limit(limit);
            }

            return await query.ToListAsync();
        }

        public async Task<List<EnrollmentSemesterSettings>> FindOverlappingSemestersAsync(
            DateTime semesterStartDate,
            DateTime semesterEndDate,
            Guid? excludeId = null)
        {
            // Find semesters that overlap with the given date range
            // Two date ranges overlap if: start1 < end2 && start2 < end1
            var filters = new List<FilterDefinition<EnrollmentSemesterSettings>>
            {
                Builders<EnrollmentSemesterSettings>.Filter.Eq(x => x.IsDeleted, false),
                // Overlap condition: semesterStartDate < existing.SemesterEndDate && existing.SemesterStartDate < semesterEndDate
                Builders<EnrollmentSemesterSettings>.Filter.Lt(x => x.SemesterStartDate, semesterEndDate),
                Builders<EnrollmentSemesterSettings>.Filter.Gt(x => x.SemesterEndDate, semesterStartDate)
            };

            if (excludeId.HasValue)
            {
                filters.Add(Builders<EnrollmentSemesterSettings>.Filter.Ne(x => x.Id, excludeId.Value));
            }

            var filter = Builders<EnrollmentSemesterSettings>.Filter.And(filters);
            return await _collection.Find(filter).ToListAsync();
        }
    }
}

