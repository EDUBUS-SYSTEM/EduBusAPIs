namespace Services.Models.Trip
{
	public class TripDto
	{
		public Guid Id { get; set; }
		public Guid RouteId { get; set; }
		public DateTime ServiceDate { get; set; }
		public DateTime PlannedStartAt { get; set; }
		public DateTime PlannedEndAt { get; set; }
		public DateTime? StartTime { get; set; }
		public DateTime? EndTime { get; set; }
		public string Status { get; set; } = string.Empty;
		public ScheduleSnapshotDto ScheduleSnapshot { get; set; } = new ScheduleSnapshotDto();
		public List<TripStopDto> Stops { get; set; } = new List<TripStopDto>();
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
	}

	public class ScheduleSnapshotDto
	{
		public Guid ScheduleId { get; set; }
		public string Name { get; set; } = string.Empty;
		public string StartTime { get; set; } = string.Empty;
		public string EndTime { get; set; } = string.Empty;
		public string RRule { get; set; } = string.Empty;
	}

	public class TripStopDto
	{
		public Guid Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public DateTime PlannedArrival { get; set; }
		public DateTime? ActualArrival { get; set; }
		public DateTime PlannedDeparture { get; set; }
		public DateTime? ActualDeparture { get; set; }
		public int Sequence { get; set; }
	}

	public class CreateTripDto
	{
		public Guid RouteId { get; set; }
		public DateTime ServiceDate { get; set; }
		public DateTime PlannedStartAt { get; set; }
		public DateTime PlannedEndAt { get; set; }
		public string Status { get; set; } = "Scheduled";
		public ScheduleSnapshotDto ScheduleSnapshot { get; set; } = new ScheduleSnapshotDto();
		public List<TripStopDto> Stops { get; set; } = new List<TripStopDto>();
	}

	public class UpdateTripDto
	{
		public Guid Id { get; set; }
		public Guid RouteId { get; set; }
		public DateTime ServiceDate { get; set; }
		public DateTime PlannedStartAt { get; set; }
		public DateTime PlannedEndAt { get; set; }
		public DateTime? StartTime { get; set; }
		public DateTime? EndTime { get; set; }
		public string Status { get; set; } = string.Empty;
		public ScheduleSnapshotDto ScheduleSnapshot { get; set; } = new ScheduleSnapshotDto();
		public List<TripStopDto> Stops { get; set; } = new List<TripStopDto>();
	}
}