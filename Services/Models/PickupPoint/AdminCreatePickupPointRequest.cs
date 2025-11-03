using System.ComponentModel.DataAnnotations;

namespace Services.Models.PickupPoint
{
    public class AdminCreatePickupPointRequest
    {
        [Required(ErrorMessage = "Parent ID is required")]
        public Guid ParentId { get; set; }

        [Required(ErrorMessage = "Student IDs are required")]
        [MinLength(1, ErrorMessage = "At least one student must be specified")]
        public List<Guid> StudentIds { get; set; } = new();

        [Required(ErrorMessage = "Address text is required")]
        [MaxLength(500, ErrorMessage = "Address text cannot exceed 500 characters")]
        public string AddressText { get; set; } = null!;

        [Required(ErrorMessage = "Latitude is required")]
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
        public double Latitude { get; set; }

        [Required(ErrorMessage = "Longitude is required")]
        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
        public double Longitude { get; set; }

        [Required(ErrorMessage = "Distance is required")]
        [Range(0.01, 1000, ErrorMessage = "Distance must be between 0.01 and 1000 km")]
        public double DistanceKm { get; set; }

        [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }
    }
}
