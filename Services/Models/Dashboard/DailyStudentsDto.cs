namespace Services.Models.Dashboard
{
    /// <summary>
    /// DTO for daily student statistics
    /// </summary>
    public class DailyStudentsDto
    {
        public int Today { get; set; }
        public int Yesterday { get; set; }
        public int ThisWeek { get; set; }
        public int ThisMonth { get; set; }
        public List<DailyStudentCount> Last7Days { get; set; } = new();
    }

    public class DailyStudentCount
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }
}
