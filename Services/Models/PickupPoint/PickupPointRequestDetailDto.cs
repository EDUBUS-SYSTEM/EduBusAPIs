namespace Services.Models.PickupPoint
{
    public class PickupPointRequestDetailDto
    {
        public Guid Id { get; set; }
        public string ParentEmail { get; set; } = string.Empty;
        
        // Parent registration information
        public ParentRegistrationInfoDto? ParentInfo { get; set; }
        
        // Student information
        public List<StudentBriefDto> Students { get; set; } = new();
        
        // Pickup point information
        public string AddressText { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double DistanceKm { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        
        // Pricing information
        public decimal UnitPriceVndPerKm { get; set; }
        public decimal EstimatedPriceVnd { get; set; }
        
        // Status and review information
        public string Status { get; set; } = "Pending";
        public string AdminNotes { get; set; } = string.Empty;
        public DateTime? ReviewedAt { get; set; }
        public Guid? ReviewedByAdminId { get; set; }
        
        // Timestamps
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ParentRegistrationInfoDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public int Gender { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
