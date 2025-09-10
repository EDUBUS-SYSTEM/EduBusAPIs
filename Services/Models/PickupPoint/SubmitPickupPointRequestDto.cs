using System.ComponentModel.DataAnnotations;

namespace Services.Models.PickupPoint
{
    public class SubmitPickupPointRequestDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [MaxLength(320)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "At least one student must be selected.")]
        [MinLength(1, ErrorMessage = "At least one student must be selected.")]
        public List<Guid> StudentIds { get; set; } = new();

        [Required(ErrorMessage = "Address is required.")]
        [MaxLength(500)]
        public string AddressText { get; set; } = string.Empty;

        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
        public double Latitude { get; set; }

        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
        public double Longitude { get; set; }

        [Range(0.01, 1000, ErrorMessage = "Distance must be between 0.01 and 1000 km.")]
        public double DistanceKm { get; set; }

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;

        [Range(1, 1_000_000_000, ErrorMessage = "Unit price must be between 1 and 1,000,000,000 VND.")]
        public decimal UnitPriceVndPerKm { get; set; } = 50_000m;

        [Range(1, 1_000_000_000, ErrorMessage = "Estimated price must be between 1 and 1,000,000,000 VND.")]
        public decimal EstimatedPriceVnd { get; set; }
    }
}
