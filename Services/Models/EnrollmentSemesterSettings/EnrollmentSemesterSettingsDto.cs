using System.ComponentModel.DataAnnotations;

namespace Services.Models.EnrollmentSemesterSettings
{
    public class EnrollmentSemesterSettingsDto
    {
        public Guid Id { get; set; }
        public string SemesterName { get; set; } = string.Empty;
        public string AcademicYear { get; set; } = string.Empty;
        public string SemesterCode { get; set; } = string.Empty;
        public DateTime SemesterStartDate { get; set; }
        public DateTime SemesterEndDate { get; set; }
        public DateTime RegistrationStartDate { get; set; }
        public DateTime RegistrationEndDate { get; set; }
        public bool IsActive { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class EnrollmentSemesterSettingsCreateDto
    {
        [Required(ErrorMessage = "Semester name is required")]
        [StringLength(200, ErrorMessage = "Semester name cannot exceed 200 characters")]
        public string SemesterName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Academic year is required")]
        [StringLength(20, ErrorMessage = "Academic year cannot exceed 20 characters")]
        public string AcademicYear { get; set; } = string.Empty;

        [Required(ErrorMessage = "Semester code is required")]
        [StringLength(50, ErrorMessage = "Semester code cannot exceed 50 characters")]
        public string SemesterCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Semester start date is required")]
        public DateTime SemesterStartDate { get; set; }

        [Required(ErrorMessage = "Semester end date is required")]
        public DateTime SemesterEndDate { get; set; }

        [Required(ErrorMessage = "Registration start date is required")]
        public DateTime RegistrationStartDate { get; set; }

        [Required(ErrorMessage = "Registration end date is required")]
        public DateTime RegistrationEndDate { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }
    }

    public class EnrollmentSemesterSettingsUpdateDto
    {
        [Required(ErrorMessage = "Registration start date is required")]
        public DateTime RegistrationStartDate { get; set; }

        [Required(ErrorMessage = "Registration end date is required")]
        public DateTime RegistrationEndDate { get; set; }

        public bool IsActive { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }
    }

    public class EnrollmentSemesterSettingsQueryResultDto
    {
        public List<EnrollmentSemesterSettingsDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PerPage { get; set; }
        public int TotalPages { get; set; }
    }
}

