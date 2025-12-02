namespace Services.Models.PickupPoint
{
    /// <summary>
    /// Response DTO for resetting pickup points by semester
    /// </summary>
    public class ResetPickupPointBySemesterResponse
    {
        public string SemesterCode { get; set; } = string.Empty;
        public string AcademicYear { get; set; } = string.Empty;
        public DateTime SemesterStartDate { get; set; }
        public DateTime SemesterEndDate { get; set; }
        
        public int TotalRecordsFound { get; set; }
        
        public int StudentsUpdated { get; set; }
        
        public int StudentsFailed { get; set; }
        
        public List<Guid> UpdatedStudentIds { get; set; } = new();
        
        public List<StudentUpdateFailure> FailedStudentIds { get; set; } = new();
        
        public string Message { get; set; } = string.Empty;
    }

    public class StudentUpdateFailure
    {
        public Guid StudentId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}

