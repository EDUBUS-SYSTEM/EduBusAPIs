namespace Data.Models;

public partial class UnitPrice : BaseDomain
{
    public string ScheduleType { get; set; } = null!; // Morning, Afternoon, FullDay
    public TimeOnly DepartureTime { get; set; }
    public decimal PricePerKm { get; set; }
    public string AcademicYear { get; set; } = null!;
    public string? SemesterCode { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public DateTime? EndTimeUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid AdminId { get; set; }

    public virtual Admin Admin { get; set; } = null!;
}
