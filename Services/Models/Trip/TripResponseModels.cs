namespace Services.Models.Trip
{
	public class StudentsForAttendanceResponse
	{
		public Guid TripId { get; set; }
		public string RouteName { get; set; } = null!;
		public DateTime ServiceDate { get; set; }
		public List<StopWithStudents> Stops { get; set; } = new();
	}

	public class StopWithStudents
	{
		public int StopId { get; set; } // SequenceOrder
		public Guid PickupPointId { get; set; }
		public string PickupPointName { get; set; } = null!;
		public int SequenceOrder { get; set; }
		public DateTime PlannedAt { get; set; }
		public DateTime? ArrivedAt { get; set; }
		public List<StudentForAttendance> Students { get; set; } = new();
	}

	public class StudentForAttendance
	{
		public Guid StudentId { get; set; }
		public string StudentName { get; set; } = null!;
		public Guid? StudentImageId { get; set; }
		public string CurrentStatus { get; set; } = "NotYetBoarded"; // "NotYetBoarded" | "Boarded" | "Alighted"
		public bool IsBoarded { get; set; }
		public bool IsAlighted { get; set; }
		public DateTime? BoardedAt { get; set; }
		public DateTime? AlightedAt { get; set; }
	}
}

