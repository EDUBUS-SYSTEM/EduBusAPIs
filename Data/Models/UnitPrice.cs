namespace Data.Models;

public partial class UnitPrice : BaseDomain
{
    public string ScheduleType { get; set; } = null!;

    public TimeOnly DepartureTime { get; set; }

    public DateTime StartTimeUtc { get; set; }

    public DateTime? EndTimeUtc { get; set; }

    public Guid AdminId { get; set; }

    public virtual Admin Admin { get; set; } = null!;
}
