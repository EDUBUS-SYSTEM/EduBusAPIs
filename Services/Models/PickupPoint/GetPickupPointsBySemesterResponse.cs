namespace Services.Models.PickupPoint
{
    /// <summary>
    /// Response DTO for getting pickup points with assigned students by semester
    /// </summary>
    public class GetPickupPointsBySemesterResponse
    {
        public string SemesterCode { get; set; } = string.Empty;
        public string AcademicYear { get; set; } = string.Empty;
        public DateTime SemesterStartDate { get; set; }
        public DateTime SemesterEndDate { get; set; }
        public string? SemesterName { get; set; }

        /// <summary>
        /// List of pickup points with their assigned students
        /// </summary>
        public List<PickupPointWithStudentsDto> PickupPoints { get; set; } = new();

        /// <summary>
        /// Total number of pickup points
        /// </summary>
        public int TotalPickupPoints { get; set; }

        /// <summary>
        /// Total number of students assigned across all pickup points
        /// </summary>
        public int TotalStudents { get; set; }
    }

    /// <summary>
    /// Pickup point with its assigned students
    /// </summary>
    public class PickupPointWithStudentsDto
    {
        public Guid PickupPointId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// List of students assigned to this pickup point for the semester
        /// </summary>
        public List<StudentAssignmentDto> Students { get; set; } = new();

        /// <summary>
        /// Number of students assigned to this pickup point
        /// </summary>
        public int StudentCount { get; set; }
    }

    /// <summary>
    /// Student assignment information
    /// </summary>
    public class StudentAssignmentDto
    {
        public Guid StudentId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}";
        public Guid? ParentId { get; set; }
        public string ParentEmail { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
        public string ChangeReason { get; set; } = string.Empty;
        public string ChangedBy { get; set; } = string.Empty;
    }
}

