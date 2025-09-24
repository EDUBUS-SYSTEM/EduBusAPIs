using System.ComponentModel.DataAnnotations;
using Data.Models.Enums;

namespace Services.Models.DriverVehicle
{
    public class UpdateAssignmentStatusRequest
    {
        [Required(ErrorMessage = "Status is required.")]
        public DriverVehicleStatus Status { get; set; }
        
        [StringLength(1000, ErrorMessage = "Note cannot exceed 1000 characters.")]
        public string? Note { get; set; }
    }
}
