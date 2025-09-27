namespace Services.Models.RouteSchedule
{
	public class RouteScheduleDto
	{
		public Guid Id { get; set; }
		public Guid RouteId { get; set; }
		public Guid ScheduleId { get; set; }
		public DateTime EffectiveFrom { get; set; }
		public DateTime? EffectiveTo { get; set; }
		public int Priority { get; set; }
		public bool IsActive { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
	}

	public class CreateRouteScheduleDto
	{
		public Guid RouteId { get; set; }
		public Guid ScheduleId { get; set; }
		public DateTime EffectiveFrom { get; set; }
		public DateTime? EffectiveTo { get; set; }
		public int Priority { get; set; } = 0;
		public bool IsActive { get; set; } = true;
	}

	public class UpdateRouteScheduleDto
	{
		public Guid Id { get; set; }
		public Guid RouteId { get; set; }
		public Guid ScheduleId { get; set; }
		public DateTime EffectiveFrom { get; set; }
		public DateTime? EffectiveTo { get; set; }
		public int Priority { get; set; }
		public bool IsActive { get; set; }
	}
}