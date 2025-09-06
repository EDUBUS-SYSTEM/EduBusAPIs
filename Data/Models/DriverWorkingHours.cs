namespace Data.Models;

public class DriverWorkingHours : BaseDomain
{
    public Guid DriverId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsAvailable { get; set; } = true;
    
    public virtual Driver Driver { get; set; } = null!;
}
