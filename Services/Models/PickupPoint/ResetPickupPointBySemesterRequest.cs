using System.ComponentModel.DataAnnotations;

namespace Services.Models.PickupPoint
{
    /// <summary>
    /// Request DTO for resetting pickup points by semester
    /// Admin selects semester (e.g., S1 2025-2026) with start and end dates
    /// System will find all StudentPickupPoint records matching the semester criteria
    /// and assign PickupPointId to CurrentPickupPointId in Student table
    /// </summary>
    public class ResetPickupPointBySemesterRequest
    {
        [Required(ErrorMessage = "Semester code is required (e.g., S1, S2)")]
        [MaxLength(20, ErrorMessage = "Semester code cannot exceed 20 characters")]
        public string SemesterCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Academic year is required (e.g., 2025-2026)")]
        [MaxLength(20, ErrorMessage = "Academic year cannot exceed 20 characters")]
        public string AcademicYear { get; set; } = string.Empty;

        [Required(ErrorMessage = "Semester start date is required")]
        public DateTime SemesterStartDate { get; set; }

        [Required(ErrorMessage = "Semester end date is required")]
        public DateTime SemesterEndDate { get; set; }

        [MaxLength(100, ErrorMessage = "Semester name cannot exceed 100 characters")]
        public string? SemesterName { get; set; }
    }
}

