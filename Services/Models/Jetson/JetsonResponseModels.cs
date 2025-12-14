namespace Services.Models.Jetson
{
	public class JetsonStudentSyncResponse
	{
		public Guid RouteId { get; set; }
		public string RouteName { get; set; } = null!;
		public int TotalStudents { get; set; }
		public DateTime SyncedAt { get; set; }
		public List<JetsonStudentData> Students { get; set; } = new();
	}

	public class JetsonStudentData
	{
		public Guid StudentId { get; set; }
		public string StudentName { get; set; } = null!;
		public string? PhotoUrl { get; set; }
		public List<float> Embedding { get; set; } = new(); // 512-dim
		public string ModelVersion { get; set; } = null!;
	}

	public class ActiveTripResponse
	{
		public Guid TripId { get; set; }
		public Guid RouteId { get; set; }
		public string LicensePlate { get; set; } = string.Empty;
		public string RouteName { get; set; } = string.Empty;
	}
}
