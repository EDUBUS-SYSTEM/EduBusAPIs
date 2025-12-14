namespace Services.Models.Trip
{
    /// <summary>
    /// DTO for supervisor to view assigned trips.
    /// Limited payload: only information required for monitoring.
    /// </summary>
    public class SupervisorTripListItemDto
    {
        public Guid Id { get; set; }
        public DateTime ServiceDate { get; set; }
        public DateTime PlannedStartAt { get; set; }
        public DateTime PlannedEndAt { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public VehicleSnapshotDto? Vehicle { get; set; }
        public DriverSnapshotDto? Driver { get; set; }
        public int TotalStops { get; set; }
        public int CompletedStops { get; set; }
    }

    /// <summary>
    /// Detailed trip DTO for supervisor.
    /// Limited payload: information sufficient for monitoring and attendance.
    /// </summary>
    public class SupervisorTripDetailDto
    {
        public Guid Id { get; set; }
        public DateTime ServiceDate { get; set; }
        public DateTime PlannedStartAt { get; set; }
        public DateTime PlannedEndAt { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        
        // Vehicle info (limited)
        public VehicleSnapshotDto? Vehicle { get; set; }
        
        // Driver info (limited)
        public DriverSnapshotDto? Driver { get; set; }
        
        // Stops with students
        public List<SupervisorTripStopDto> Stops { get; set; } = new List<SupervisorTripStopDto>();
    }

    /// <summary>
    /// Trip stop DTO for supervisor view.
    /// </summary>
    public class SupervisorTripStopDto
    {
        public Guid PickupPointId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime PlannedArrival { get; set; }
        public DateTime? ActualArrival { get; set; }
        public DateTime PlannedDeparture { get; set; }
        public DateTime? ActualDeparture { get; set; }
        public int Sequence { get; set; }
        public StopLocationDto Location { get; set; } = new();
        public List<SupervisorAttendanceDto> Attendance { get; set; } = new();
    }

    /// <summary>
    /// Attendance DTO for supervisor view.
    /// Contains only basic, non-sensitive information.
    /// </summary>
    public class SupervisorAttendanceDto
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public DateTime? BoardedAt { get; set; }
        public DateTime? AlightedAt { get; set; }
        public string State { get; set; } = string.Empty;
        public string? BoardStatus { get; set; }
        public string? AlightStatus { get; set; }
    }
}
