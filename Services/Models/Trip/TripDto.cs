using Constants;
using MongoDB.Bson.Serialization.Attributes;

namespace Services.Models.Trip
{
	public class TripDto
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
		public Guid VehicleId { get; set; }
		public Guid? DriverVehicleId { get; set; }
		public Guid? SupervisorVehicleId { get; set; }
		public VehicleSnapshotDto? Vehicle { get; set; }
		public DriverSnapshotDto? Driver { get; set; }
		public TripLocationDto? CurrentLocation { get; set; }
		public SupervisorSnapshotDto? Supervisor { get; set; }
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

	public class VehicleSnapshotDto
	{
		public Guid Id { get; set; }
		public string MaskedPlate { get; set; } = string.Empty;
		public int Capacity { get; set; }
		public string Status { get; set; } = string.Empty;
	}

	public class DriverSnapshotDto
	{
		public Guid Id { get; set; }
		public string FullName { get; set; } = string.Empty;
		public string Phone { get; set; } = string.Empty;
		public bool IsPrimary { get; set; }
		public DateTime SnapshottedAtUtc { get; set; }
	}

	public class SupervisorSnapshotDto
	{
		public Guid Id { get; set; }
		public string FullName { get; set; } = string.Empty;
		public string Phone { get; set; } = string.Empty;
		public DateTime SnapshottedAtUtc { get; set; }
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
		public StopLocationDto Location { get; set; } = new StopLocationDto();
        public List<ParentAttendanceDto> Attendance { get; set; } = new List<ParentAttendanceDto>();
		
	}

	public class CreateTripDto
	{
		public Guid RouteId { get; set; }
		public DateTime ServiceDate { get; set; }
		public DateTime PlannedStartAt { get; set; }
		public DateTime PlannedEndAt { get; set; }
		public string Status { get; set; } = Constants.TripStatus.Scheduled;
		public Guid VehicleId { get; set; }
		public Guid? DriverVehicleId { get; set; }
		public VehicleSnapshotDto? Vehicle { get; set; }
		public DriverSnapshotDto? Driver { get; set; }
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
		public Guid VehicleId { get; set; }
		public Guid? DriverVehicleId { get; set; }
		public VehicleSnapshotDto? Vehicle { get; set; }
		public DriverSnapshotDto? Driver { get; set; }
		public ScheduleSnapshotDto ScheduleSnapshot { get; set; } = new ScheduleSnapshotDto();
		public List<TripStopDto> Stops { get; set; } = new List<TripStopDto>();
	}

	public class SimpleTripDto
	{
		public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
		public DateTime PlannedStartAt { get; set; }
		public DateTime PlannedEndAt { get; set; }
		public string PlateVehicle { get; set; } = string.Empty;
		public string Status { get; set; } = string.Empty;
        public int TotalStops { get; set; }
        public int CompletedStops { get; set; }
    }

	public class ParentTripDto
	{
		public Guid Id { get; set; }
		public DateTime ServiceDate { get; set; }
		public DateTime PlannedStartAt { get; set; }
		public DateTime PlannedEndAt { get; set; }
		public DateTime? StartTime { get; set; }
		public DateTime? EndTime { get; set; }
		public string Status { get; set; } = string.Empty;
		public VehicleSnapshotDto? Vehicle { get; set; }
		public DriverSnapshotDto? Driver { get; set; }
		public ScheduleSnapshotDto ScheduleSnapshot { get; set; } = new ScheduleSnapshotDto();
		public List<ParentTripStopDto> Stops { get; set; } = new List<ParentTripStopDto>();
	}

	public class ParentTripStopDto
	{
		public Guid Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public DateTime PlannedArrival { get; set; }
		public DateTime? ActualArrival { get; set; }
		public DateTime PlannedDeparture { get; set; }
		public DateTime? ActualDeparture { get; set; }
		public int Sequence { get; set; }
		public List<ParentAttendanceDto> Attendance { get; set; } = new List<ParentAttendanceDto>();
	}

	public class ParentAttendanceDto
	{
		public Guid StudentId { get; set; }
		public string StudentName { get; set; } = string.Empty;
		public DateTime? BoardedAt { get; set; }
		public string State { get; set; } = string.Empty;
	}

	public class TripLocationDto
	{
		public double Latitude { get; set; }
		public double Longitude { get; set; }
		public DateTime RecordedAt { get; set; }
		public double? Speed { get; set; }
		public double? Accuracy { get; set; }
		public bool IsMoving { get; set; }
	}
    public class StopLocationDto
    {
        public double Latitude { get; set; }

        public double Longitude { get; set; }
        public string Address { get; set; } = string.Empty;
    }
}