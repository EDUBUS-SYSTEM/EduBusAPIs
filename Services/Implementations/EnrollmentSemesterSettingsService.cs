using Data.Models;
using Data.Repos.Interfaces;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Services.Contracts;
using Services.Models.EnrollmentSemesterSettings;
using Utils;

namespace Services.Implementations
{
    public class EnrollmentSemesterSettingsService : IEnrollmentSemesterSettingsService
    {
        private readonly IDatabaseFactory _databaseFactory;
        private readonly ILogger<EnrollmentSemesterSettingsService> _logger;

        public EnrollmentSemesterSettingsService(
            IDatabaseFactory databaseFactory,
            ILogger<EnrollmentSemesterSettingsService> logger)
        {
            _databaseFactory = databaseFactory;
            _logger = logger;
        }

        public async Task<EnrollmentSemesterSettings?> GetByIdAsync(Guid id)
        {
            try
            {
                var repository = _databaseFactory.GetRepositoryByType<IEnrollmentSemesterSettingsRepository>(DatabaseType.MongoDb);
                return await repository.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enrollment semester settings with id: {Id}", id);
                throw;
            }
        }

        public async Task<EnrollmentSemesterSettings?> GetActiveSettingsAsync()
        {
            try
            {
                var repository = _databaseFactory.GetRepositoryByType<IEnrollmentSemesterSettingsRepository>(DatabaseType.MongoDb);
                return await repository.GetActiveSettingsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active enrollment semester settings");
                throw;
            }
        }

        public async Task<EnrollmentSemesterSettings?> GetCurrentOpenRegistrationAsync()
        {
            try
            {
                var repository = _databaseFactory.GetRepositoryByType<IEnrollmentSemesterSettingsRepository>(DatabaseType.MongoDb);
                return await repository.GetCurrentOpenRegistrationAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current open registration");
                throw;
            }
        }

        public async Task<EnrollmentSemesterSettings?> FindBySemesterCodeAsync(string semesterCode)
        {
            try
            {
                var repository = _databaseFactory.GetRepositoryByType<IEnrollmentSemesterSettingsRepository>(DatabaseType.MongoDb);
                return await repository.FindBySemesterCodeAsync(semesterCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding enrollment semester settings by code: {SemesterCode}", semesterCode);
                throw;
            }
        }

        public async Task<List<EnrollmentSemesterSettings>> FindByAcademicYearAsync(string academicYear)
        {
            try
            {
                var repository = _databaseFactory.GetRepositoryByType<IEnrollmentSemesterSettingsRepository>(DatabaseType.MongoDb);
                return await repository.FindByAcademicYearAsync(academicYear);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding enrollment semester settings by academic year: {AcademicYear}", academicYear);
                throw;
            }
        }

        public async Task<EnrollmentSemesterSettings> CreateAsync(EnrollmentSemesterSettingsCreateDto createDto)
        {
            try
            {
                ValidateCreateDto(createDto);

                var repository = _databaseFactory.GetRepositoryByType<IEnrollmentSemesterSettingsRepository>(DatabaseType.MongoDb);

                var existing = await repository.FindBySemesterCodeAsync(createDto.SemesterCode);
                if (existing != null && !existing.IsDeleted)
                {
                    throw new InvalidOperationException($"Enrollment semester settings with code '{createDto.SemesterCode}' already exists.");
                }

                // Normalize dates before creating entity
                var semesterStartDate = NormalizeDate(createDto.SemesterStartDate);
                var semesterEndDate = NormalizeDate(createDto.SemesterEndDate);
                var registrationStartDate = NormalizeDate(createDto.RegistrationStartDate);
                var registrationEndDate = NormalizeDate(createDto.RegistrationEndDate);

                var settings = new EnrollmentSemesterSettings
                {
                    Id = Guid.NewGuid(),
                    SemesterName = createDto.SemesterName,
                    AcademicYear = createDto.AcademicYear,
                    SemesterCode = createDto.SemesterCode,
                    SemesterStartDate = semesterStartDate,
                    SemesterEndDate = semesterEndDate,
                    RegistrationStartDate = registrationStartDate,
                    RegistrationEndDate = registrationEndDate,
                    IsActive = createDto.IsActive,
                    Description = createDto.Description,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                ValidateSettings(settings);

                // Check for overlapping semesters
                var overlapping = await repository.FindOverlappingSemestersAsync(
                    settings.SemesterStartDate,
                    settings.SemesterEndDate);
                
                if (overlapping.Any())
                {
                    var overlappingCodes = string.Join(", ", overlapping.Select(s => s.SemesterCode));
                    throw new InvalidOperationException(
                        $"The semester period overlaps with existing semester(s): {overlappingCodes}. " +
                        $"Please ensure the semester dates do not overlap with other active semesters.");
                }

                return await repository.AddAsync(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating enrollment semester settings");
                throw;
            }
        }

        public async Task<EnrollmentSemesterSettings?> UpdateAsync(Guid id, EnrollmentSemesterSettingsUpdateDto updateDto)
        {
            try
            {
                var repository = _databaseFactory.GetRepositoryByType<IEnrollmentSemesterSettingsRepository>(DatabaseType.MongoDb);
                var existing = await repository.FindAsync(id);
                if (existing == null || existing.IsDeleted)
                {
                    return null;
                }

                // Validate update DTO with existing semester dates
                ValidateUpdateDto(updateDto, existing);

                // Normalize dates before updating entity
                var registrationStartDate = NormalizeDate(updateDto.RegistrationStartDate);
                var registrationEndDate = NormalizeDate(updateDto.RegistrationEndDate);

                // Only update allowed fields: registration dates, isActive, and description
                existing.RegistrationStartDate = registrationStartDate;
                existing.RegistrationEndDate = registrationEndDate;
                existing.IsActive = updateDto.IsActive;
                existing.Description = updateDto.Description;
                existing.UpdatedAt = DateTime.UtcNow;

                // Validate the updated entity
                ValidateSettings(existing);

                return await repository.UpdateAsync(existing);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating enrollment semester settings with id: {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var repository = _databaseFactory.GetRepositoryByType<IEnrollmentSemesterSettingsRepository>(DatabaseType.MongoDb);
                var deleted = await repository.DeleteAsync(id);
                return deleted != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting enrollment semester settings with id: {Id}", id);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            try
            {
                var repository = _databaseFactory.GetRepositoryByType<IEnrollmentSemesterSettingsRepository>(DatabaseType.MongoDb);
                return await repository.ExistsAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if enrollment semester settings exists: {Id}", id);
                throw;
            }
        }

        public async Task<QueryResult<EnrollmentSemesterSettings>> QueryAsync(
            string? semesterCode = null,
            string? academicYear = null,
            bool? isActive = null,
            string? searchTerm = null,
            int page = 1,
            int perPage = 20,
            string? sortBy = null,
            string sortOrder = "desc")
        {
            try
            {
                if (page < 1) page = 1;
                if (perPage < 1) perPage = 20;

                var repository = _databaseFactory.GetRepositoryByType<IEnrollmentSemesterSettingsRepository>(DatabaseType.MongoDb);

                var sortDescending = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
                var skip = (page - 1) * perPage;

                var items = await repository.QueryAsync(
                    semesterCode,
                    academicYear,
                    isActive,
                    searchTerm,
                    skip,
                    perPage,
                    sortBy,
                    sortDescending);

                var totalCount = await GetTotalCountAsync(repository, semesterCode, academicYear, isActive, searchTerm);

                return new QueryResult<EnrollmentSemesterSettings>
                {
                    Items = items,
                    TotalCount = totalCount,
                    Page = page,
                    PerPage = perPage
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying enrollment semester settings");
                throw;
            }
        }

        private async Task<int> GetTotalCountAsync(
            IEnrollmentSemesterSettingsRepository repository,
            string? semesterCode,
            string? academicYear,
            bool? isActive,
            string? searchTerm)
        {
            var items = await repository.QueryAsync(semesterCode, academicYear, isActive, searchTerm, 0, 0, null, true);
            return items.Count;
        }

        private static void ValidateCreateDto(EnrollmentSemesterSettingsCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.SemesterName))
                throw new ArgumentException("Semester name is required");

            if (string.IsNullOrWhiteSpace(dto.AcademicYear))
                throw new ArgumentException("Academic year is required");

            if (string.IsNullOrWhiteSpace(dto.SemesterCode))
                throw new ArgumentException("Semester code is required");

            // Normalize dates to UTC and remove time component (keep only date)
            var semesterStartDate = NormalizeDate(dto.SemesterStartDate);
            var semesterEndDate = NormalizeDate(dto.SemesterEndDate);
            var registrationStartDate = NormalizeDate(dto.RegistrationStartDate);
            var registrationEndDate = NormalizeDate(dto.RegistrationEndDate);

            // Validate dates are not default (01/01/0001)
            ValidateDateNotDefault(semesterStartDate, "Semester start date");
            ValidateDateNotDefault(semesterEndDate, "Semester end date");
            ValidateDateNotDefault(registrationStartDate, "Registration start date");
            ValidateDateNotDefault(registrationEndDate, "Registration end date");

            // Validate dates are within reasonable range (not too far in past/future)
            var minDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var maxDate = new DateTime(2100, 12, 31, 23, 59, 59, DateTimeKind.Utc);

            if (semesterStartDate < minDate || semesterStartDate > maxDate)
                throw new ArgumentException($"Semester start date must be between {minDate:yyyy-MM-dd} and {maxDate:yyyy-MM-dd}");

            if (semesterEndDate < minDate || semesterEndDate > maxDate)
                throw new ArgumentException($"Semester end date must be between {minDate:yyyy-MM-dd} and {maxDate:yyyy-MM-dd}");

            if (registrationStartDate < minDate || registrationStartDate > maxDate)
                throw new ArgumentException($"Registration start date must be between {minDate:yyyy-MM-dd} and {maxDate:yyyy-MM-dd}");

            if (registrationEndDate < minDate || registrationEndDate > maxDate)
                throw new ArgumentException($"Registration end date must be between {minDate:yyyy-MM-dd} and {maxDate:yyyy-MM-dd}");

            // Validate date relationships
            ValidateDateRelationships(
                semesterStartDate,
                semesterEndDate,
                registrationStartDate,
                registrationEndDate);
        }

        private static void ValidateUpdateDto(EnrollmentSemesterSettingsUpdateDto dto, EnrollmentSemesterSettings existing)
        {
            // Normalize dates to UTC and remove time component (keep only date)
            var registrationStartDate = NormalizeDate(dto.RegistrationStartDate);
            var registrationEndDate = NormalizeDate(dto.RegistrationEndDate);

            // Validate dates are not default (01/01/0001)
            ValidateDateNotDefault(registrationStartDate, "Registration start date");
            ValidateDateNotDefault(registrationEndDate, "Registration end date");

            // Validate dates are within reasonable range (not too far in past/future)
            var minDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var maxDate = new DateTime(2100, 12, 31, 23, 59, 59, DateTimeKind.Utc);

            if (registrationStartDate < minDate || registrationStartDate > maxDate)
                throw new ArgumentException($"Registration start date must be between {minDate:yyyy-MM-dd} and {maxDate:yyyy-MM-dd}");

            if (registrationEndDate < minDate || registrationEndDate > maxDate)
                throw new ArgumentException($"Registration end date must be between {minDate:yyyy-MM-dd} and {maxDate:yyyy-MM-dd}");

            // Validate date relationships using existing semester dates
            ValidateDateRelationships(
                existing.SemesterStartDate,
                existing.SemesterEndDate,
                registrationStartDate,
                registrationEndDate);
        }

        private static void ValidateDateNotDefault(DateTime date, string fieldName)
        {
            if (date == default(DateTime) || date == DateTime.MinValue)
                throw new ArgumentException($"{fieldName} cannot be default or empty");
        }

        private static DateTime NormalizeDate(DateTime date)
        {
            // Convert to UTC if not already
            var utcDate = date.Kind == DateTimeKind.Utc 
                ? date 
                : date.Kind == DateTimeKind.Local 
                    ? date.ToUniversalTime() 
                    : DateTime.SpecifyKind(date, DateTimeKind.Utc);

            return new DateTime(utcDate.Year, utcDate.Month, utcDate.Day, 0, 0, 0, DateTimeKind.Utc);
        }

        private static void ValidateDateRelationships(
            DateTime semesterStartDate,
            DateTime semesterEndDate,
            DateTime registrationStartDate,
            DateTime registrationEndDate)
        {
            // Ensure all dates are UTC for proper comparison
            if (semesterStartDate.Kind != DateTimeKind.Utc)
                throw new ArgumentException("Semester start date must be in UTC");
            if (semesterEndDate.Kind != DateTimeKind.Utc)
                throw new ArgumentException("Semester end date must be in UTC");
            if (registrationStartDate.Kind != DateTimeKind.Utc)
                throw new ArgumentException("Registration start date must be in UTC");
            if (registrationEndDate.Kind != DateTimeKind.Utc)
                throw new ArgumentException("Registration end date must be in UTC");

            // Semester end date must be after semester start date
            if (semesterEndDate <= semesterStartDate)
                throw new ArgumentException("Semester end date must be greater than semester start date");

            // Registration end date must be after registration start date
            if (registrationEndDate <= registrationStartDate)
                throw new ArgumentException("Registration end date must be greater than registration start date");

            // Registration period must be at least 1 day
            var registrationPeriod = registrationEndDate - registrationStartDate;
            if (registrationPeriod.TotalDays < 1)
                throw new ArgumentException("Registration period must be at least 1 day");

            // Registration period should not be too long (e.g., more than 1 year)
            var maxRegistrationPeriod = TimeSpan.FromDays(365);
            if (registrationPeriod > maxRegistrationPeriod)
                throw new ArgumentException("Registration period cannot exceed 365 days");

            // Both registration start date and end date must be before semester start date
            if (registrationStartDate >= semesterStartDate)
                throw new ArgumentException("Registration start date must be before semester start date");

            if (registrationEndDate >= semesterStartDate)
                throw new ArgumentException("Registration end date must be before semester start date");

            // Registration period should end before or on semester end date
            if (registrationEndDate > semesterEndDate)
                throw new ArgumentException("Registration end date must not be after semester end date");

            // Semester period should be reasonable (e.g., between 1 month and 1 year)
            var minSemesterPeriod = TimeSpan.FromDays(30);
            var maxSemesterPeriod = TimeSpan.FromDays(365);
            var semesterPeriod = semesterEndDate - semesterStartDate;
            
            if (semesterPeriod < minSemesterPeriod)
                throw new ArgumentException($"Semester period must be at least {minSemesterPeriod.Days} days");

            if (semesterPeriod > maxSemesterPeriod)
                throw new ArgumentException($"Semester period cannot exceed {maxSemesterPeriod.Days} days");

            if (registrationPeriod > semesterPeriod)
                throw new ArgumentException("Registration period cannot be longer than the semester period");
        }

        private static void ValidateSettings(EnrollmentSemesterSettings settings)
        {
            // Validate dates are not default
            ValidateDateNotDefault(settings.SemesterStartDate, "Semester start date");
            ValidateDateNotDefault(settings.SemesterEndDate, "Semester end date");
            ValidateDateNotDefault(settings.RegistrationStartDate, "Registration start date");
            ValidateDateNotDefault(settings.RegistrationEndDate, "Registration end date");

            // Validate date relationships
            ValidateDateRelationships(
                settings.SemesterStartDate,
                settings.SemesterEndDate,
                settings.RegistrationStartDate,
                settings.RegistrationEndDate);
        }
    }
}

