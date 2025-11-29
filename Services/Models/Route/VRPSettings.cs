namespace Services.Models.Route
{
    public class VRPSettings
    {
        public int DefaultTimeLimitSeconds { get; set; } = 60;
        public bool UseTimeWindows { get; set; } = true;
        public string OptimizationType { get; set; } = "Distance";
        public int ServiceTimeSeconds { get; set; } = 210; // 3.5 minutes per pickup point
        public double AverageSpeedKmh { get; set; } = 45.0; // Average speed for travel time calculation
        public int SlackTimeSeconds { get; set; } = 300; // 5 minutes slack for time dimension
        public int MaxRouteDurationSeconds { get; set; } = 5400; // 1.5 hours max route duration
        public double SchoolLatitude { get; set; } 
        public double SchoolLongitude { get; set; }
		public string Engine { get; set; } = "OrTools";
	}
}
