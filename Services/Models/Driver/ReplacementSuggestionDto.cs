namespace Services.Models.Driver
{
    public class ReplacementSuggestionDto
    {
        public Guid Id { get; set; }
        public Guid DriverId { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public string DriverEmail { get; set; } = string.Empty;
        public string DriverPhone { get; set; } = string.Empty;
        public Guid VehicleId { get; set; }
        public string VehiclePlate { get; set; } = string.Empty;
        public int VehicleCapacity { get; set; }
        public double Score { get; set; } // 0-100
        public string Reason { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        
        // Driver qualifications
        public bool HasValidLicense { get; set; }
        public bool HasHealthCertificate { get; set; }
        public int YearsOfExperience { get; set; }
        
        // Availability
        public bool IsAvailable { get; set; }
        public string? AvailabilityNote { get; set; }
    }
}
