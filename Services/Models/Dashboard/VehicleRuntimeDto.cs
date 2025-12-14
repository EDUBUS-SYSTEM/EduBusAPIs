namespace Services.Models.Dashboard
{

    public class VehicleRuntimeDto
    {
        public double TotalHoursToday { get; set; }
        public double AverageHoursPerTrip { get; set; }
        public int TotalTripsToday { get; set; }
        public List<VehicleUsage> TopVehicles { get; set; } = new();
    }

    public class VehicleUsage
    {
        public Guid VehicleId { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public double TotalHours { get; set; }
        public int TripCount { get; set; }
    }
}
