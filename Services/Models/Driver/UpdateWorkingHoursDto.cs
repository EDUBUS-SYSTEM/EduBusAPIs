namespace Services.Models.Driver
{
    public class UpdateWorkingHoursDto
    {
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public bool? IsAvailable { get; set; }
    }
}
