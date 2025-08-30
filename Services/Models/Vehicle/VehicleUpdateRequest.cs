using Data.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Services.Models.Vehicle
{
    public class VehicleUpdateRequest
    {
        [RegularExpression(@"^[0-9]{2}[A-Z]-[0-9]{4,5}(\.[0-9]{2})?$",
        ErrorMessage = "Invalid Vietnam license plate format")]
        public string LicensePlate { get; set; } = null!;

        [Required]
        [EnumDataType(typeof(VehicleCapacity), ErrorMessage = "Capacity must be 16, 24 or 32")]
        public VehicleCapacity Capacity { get; set; }

        [Required]
        [EnumDataType(typeof(VehicleStatus))]
        public VehicleStatus Status { get; set; }
    }
}
