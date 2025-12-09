namespace Services.Models.Dashboard
{
    public class ActiveSemesterDto
    {
        public string SemesterName { get; set; } = string.Empty;
        public string SemesterCode { get; set; } = string.Empty;
        public string AcademicYear { get; set; } = string.Empty;
        public DateTime SemesterStartDate { get; set; }
        public DateTime SemesterEndDate { get; set; }
    }
}


