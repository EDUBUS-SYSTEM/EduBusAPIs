namespace Services.Models.Route
{
    public class VRPSettings
    {
        public int DefaultTimeLimitSeconds { get; set; } = 60;
        public bool UseTimeWindows { get; set; } = true;
        public string OptimizationType { get; set; } = "Distance";
        public int ServiceTimeSeconds { get; set; } = 300;
        public double SchoolLatitude { get; set; } 
        public double SchoolLongitude { get; set; }
    }
}
