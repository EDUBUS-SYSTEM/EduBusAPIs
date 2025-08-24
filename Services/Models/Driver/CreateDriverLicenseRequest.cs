using System;
using System.ComponentModel.DataAnnotations;

namespace Services.Models.Driver
{
    public class CreateDriverLicenseRequest
    {
        [Required(ErrorMessage = "License number is required.")]
        [StringLength(20, MinimumLength = 5, ErrorMessage = "License number must be between 5 and 20 characters.")]
        public string LicenseNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Date of issue is required.")]
        public DateTime DateOfIssue { get; set; }

        [Required(ErrorMessage = "Issued by is required.")]
        [StringLength(200, ErrorMessage = "Issued by must not exceed 200 characters.")]
        public string IssuedBy { get; set; } = string.Empty;

        [Required(ErrorMessage = "Driver ID is required.")]
        public Guid DriverId { get; set; }
    }
}
