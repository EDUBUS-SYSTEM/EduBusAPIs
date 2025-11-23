using Data.Models;
using Services.Models.EnrollmentSemesterSettings;

namespace Services.Contracts
{
    public interface IEnrollmentSemesterSettingsService
    {
        Task<EnrollmentSemesterSettings?> GetByIdAsync(Guid id);
        Task<EnrollmentSemesterSettings?> GetActiveSettingsAsync();
        Task<EnrollmentSemesterSettings?> GetCurrentOpenRegistrationAsync();
        Task<EnrollmentSemesterSettings?> FindBySemesterCodeAsync(string semesterCode);
        Task<List<EnrollmentSemesterSettings>> FindByAcademicYearAsync(string academicYear);
        Task<EnrollmentSemesterSettings> CreateAsync(EnrollmentSemesterSettingsCreateDto createDto);
        Task<EnrollmentSemesterSettings?> UpdateAsync(Guid id, EnrollmentSemesterSettingsUpdateDto updateDto);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<QueryResult<EnrollmentSemesterSettings>> QueryAsync(
            string? semesterCode = null,
            string? academicYear = null,
            bool? isActive = null,
            string? searchTerm = null,
            int page = 1,
            int perPage = 20,
            string? sortBy = null,
            string sortOrder = "desc");
    }

    public class QueryResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PerPage { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PerPage);
    }
}

