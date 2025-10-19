namespace Services.Models.Trip
{
    public class DriverScheduleDto
    {
        public Guid Id { get; set; }
        public Guid RouteId { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public DateTime ServiceDate { get; set; }
        public DateTime PlannedStartAt { get; set; }
        public DateTime PlannedEndAt { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ScheduleName { get; set; } = string.Empty;
        public int TotalStops { get; set; }
        public int CompletedStops { get; set; }
        public bool IsOverride { get; set; }
        public string OverrideReason { get; set; } = string.Empty;
        public List<DriverScheduleStopDto> Stops { get; set; } = new List<DriverScheduleStopDto>();
    }

    public class DriverScheduleStopDto
    {
        public int SequenceOrder { get; set; }
        public Guid PickupPointId { get; set; }
        public string PickupPointName { get; set; } = string.Empty;
        public DateTime PlannedAt { get; set; }
        public DateTime? ArrivedAt { get; set; }
        public DateTime? DepartedAt { get; set; }
        public string Address { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int TotalStudents { get; set; }
        public int PresentStudents { get; set; }
        public int AbsentStudents { get; set; }
    }
}
