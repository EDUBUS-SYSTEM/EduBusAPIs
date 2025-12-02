using Data.Models;

namespace Data.Repos.Interfaces
{
    public interface IEnrollmentSemesterSettingsRepository : IMongoRepository<EnrollmentSemesterSettings>
    {
        Task<EnrollmentSemesterSettings?> GetActiveSettingsAsync();

        Task<EnrollmentSemesterSettings?> FindBySemesterCodeAsync(string semesterCode);

        Task<List<EnrollmentSemesterSettings>> FindByAcademicYearAsync(string academicYear);

        Task<EnrollmentSemesterSettings?> GetCurrentOpenRegistrationAsync();

        Task<List<EnrollmentSemesterSettings>> QueryAsync(
            string? semesterCode = null,
            string? academicYear = null,
            bool? isActive = null,
            string? searchTerm = null,
            int skip = 0,
            int limit = 0,
            string? sortBy = null,
            bool sortDescending = true);

        Task<List<EnrollmentSemesterSettings>> FindOverlappingSemestersAsync(
            DateTime semesterStartDate,
            DateTime semesterEndDate,
            Guid? excludeId = null);
    }
}

