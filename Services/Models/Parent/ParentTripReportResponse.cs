namespace Services.Models.Parent
{
    public class ParentTripReportResponse
    {
        public string SemesterId { get; set; } = string.Empty;
        public string SemesterName { get; set; } = string.Empty;
        public string SemesterCode { get; set; } = string.Empty;
        public string AcademicYear { get; set; } = string.Empty;
        public DateTime SemesterStartDate { get; set; }
        public DateTime SemesterEndDate { get; set; }
        public int TotalStudentsRegistered { get; set; }
        public decimal TotalAmountPaid { get; set; }
        public decimal TotalAmountPending { get; set; }
        public int TotalTrips { get; set; }
        public int CompletedTrips { get; set; }
        public int ScheduledTrips { get; set; }
        public int CancelledTrips { get; set; }
        public List<StudentTripStatistics> StudentStatistics { get; set; } = new();
    }

    public class StudentTripStatistics
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public decimal AmountPaid { get; set; }
        public decimal AmountPending { get; set; }
        public int TotalAttendanceRecords { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public double AttendanceRate { get; set; }
        public int TotalTripsForStudent { get; set; }
        public int CompletedTripsForStudent { get; set; }
        public int UpcomingTripsForStudent { get; set; }
    }
}

