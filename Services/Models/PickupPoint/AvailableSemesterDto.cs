namespace Services.Models.PickupPoint
{
    /// <summary>
    /// DTO for available semester from StudentPickupPoint table
    /// </summary>
    public class AvailableSemesterDto
    {
        public string SemesterCode { get; set; } = string.Empty;
        public string AcademicYear { get; set; } = string.Empty;
        public DateTime SemesterStartDate { get; set; }
        public DateTime SemesterEndDate { get; set; }
        public string? SemesterName { get; set; }
        
        /// <summary>
        /// Number of students with pickup point assignments for this semester
        /// </summary>
        public int StudentCount { get; set; }
        
        /// <summary>
        /// Display label for the semester (e.g., "S1 2025-2026")
        /// </summary>
        public string DisplayLabel => $"{SemesterCode} {AcademicYear}" + 
            (!string.IsNullOrWhiteSpace(SemesterName) ? $" - {SemesterName}" : "");
    }

    /// <summary>
    /// Response DTO for getting available semesters
    /// </summary>
    public class GetAvailableSemestersResponse
    {
        public List<AvailableSemesterDto> Semesters { get; set; } = new();
        public int TotalCount { get; set; }
    }
}

