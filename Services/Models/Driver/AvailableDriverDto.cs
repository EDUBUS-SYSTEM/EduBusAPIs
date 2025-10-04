using Data.Models.Enums;

namespace Services.Models.Driver
{
    public class GetAvailableDriverDto
    {
        public Guid Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;

        public DriverStatus Status { get; set; }

        public string? LicenseNumber { get; set; }

        public DateTime? LicenseExpiryDate { get; set; }
        public bool HasValidLicense { get; set; }

        public bool HasHealthCertificate { get; set; }

        public int YearsOfExperience { get; set; }

        public DateTime? LastActiveDate { get; set; }

        public bool IsAvailable { get; set; } = true;

        public string? AvailabilityReason { get; set; }

        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    }
}
