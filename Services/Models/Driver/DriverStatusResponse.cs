using Data.Models.Enums;

namespace Services.Models.Driver
{
    public class DriverStatusResponse
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public DriverStatus Status { get; set; }
        public string? StatusNote { get; set; }
        public DateTime? LastActiveDate { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
