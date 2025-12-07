namespace Services.Models.Dashboard
{

    public class AttendanceRateDto
    {
        public double TodayRate { get; set; }
        public double WeekRate { get; set; }
        public double MonthRate { get; set; }
        
   
        public int TotalStudents { get; set; }
        public int TotalPresent { get; set; }
        public int TotalAbsent { get; set; }
        public int TotalLate { get; set; }
        public int TotalExcused { get; set; }
        public int TotalPending { get; set; }
    }
}
