using Data.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Services.Models.Vehicle
{
    public class VehicleCreateRequest
    {
        [Required(ErrorMessage = "License plate is required")]
        [RegularExpression(@"^[0-9]{2}[A-Z]-[0-9]{4,5}(\.[0-9]{2})?$",
        ErrorMessage = "Invalid Vietnam license plate format (e.g., 43A-12345 or 30F-123.45)")]
        public string LicensePlate { get; set; } = null!;

        [Required(ErrorMessage = "Capacity is required")]
        [EnumDataType(typeof(VehicleCapacity), ErrorMessage = "Capacity must be 16, 24 or 32")]
        public VehicleCapacity Capacity { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [EnumDataType(typeof(VehicleStatus), ErrorMessage = "Invalid status value")]
        public VehicleStatus Status { get; set; }
    }
}
