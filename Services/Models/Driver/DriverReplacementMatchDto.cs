namespace Services.Models.Driver
{
    /// <summary>
    /// DTO for matching driver-vehicle assignments with active replacements
    /// Used by UI to show replacement info icon only for relevant assignments
    /// </summary>
    public class DriverReplacementMatchDto
    {
        public Guid DriverId { get; set; }
        public Guid? VehicleId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}

