namespace Services.Models.Dashboard
{
    /// <summary>
    /// DTO for attendance rate statistics
    /// </summary>
    public class AttendanceRateDto
    {
        public double TodayRate { get; set; }
        public double WeekRate { get; set; }
        public double MonthRate { get; set; }
        
        // Breakdown for today
        public int TotalStudents { get; set; }
        public int TotalPresent { get; set; }
        public int TotalAbsent { get; set; }
        public int TotalLate { get; set; }
        public int TotalExcused { get; set; }
        public int TotalPending { get; set; }
    }
}
