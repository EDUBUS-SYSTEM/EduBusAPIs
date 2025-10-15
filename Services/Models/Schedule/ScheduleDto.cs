using Data.Models;

namespace Services.Models.Schedule
{
	public class ScheduleDto
	{
		public Guid Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string StartTime { get; set; } = string.Empty;
		public string EndTime { get; set; } = string.Empty;
		public string RRule { get; set; } = string.Empty;
		public string Timezone { get; set; } = string.Empty;
		public string AcademicYear { get; set; } = string.Empty;
		public DateTime EffectiveFrom { get; set; }
		public DateTime? EffectiveTo { get; set; }
		public List<DateTime> Exceptions { get; set; } = new List<DateTime>();
		public string ScheduleType { get; set; } = string.Empty;
		public bool IsActive { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
	}

	public class CreateScheduleDto
	{
		public string Name { get; set; } = string.Empty;
		public string StartTime { get; set; } = string.Empty;
		public string EndTime { get; set; } = string.Empty;
		public string RRule { get; set; } = string.Empty;
		public string Timezone { get; set; } = string.Empty;
		public string AcademicYear { get; set; } = string.Empty;
		public DateTime EffectiveFrom { get; set; }
		public DateTime? EffectiveTo { get; set; }
		public List<DateTime> Exceptions { get; set; } = new List<DateTime>();
		public string ScheduleType { get; set; } = string.Empty;
		public bool IsActive { get; set; } = true;
	}

	public class UpdateScheduleDto
	{
		public Guid Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string StartTime { get; set; } = string.Empty;
		public string EndTime { get; set; } = string.Empty;
		public string RRule { get; set; } = string.Empty;
		public string Timezone { get; set; } = string.Empty;
		public string AcademicYear { get; set; } = string.Empty;
		public DateTime EffectiveFrom { get; set; }
		public DateTime? EffectiveTo { get; set; }
		public List<DateTime> Exceptions { get; set; } = new List<DateTime>();
		public string ScheduleType { get; set; } = string.Empty;
		public bool IsActive { get; set; }
		public List<ScheduleTimeOverride> TimeOverrides { get; set; } = new List<ScheduleTimeOverride>();
		public DateTime? UpdatedAt { get; set; }
	}
}