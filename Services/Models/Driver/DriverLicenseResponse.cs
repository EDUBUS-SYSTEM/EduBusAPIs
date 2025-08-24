using System;

namespace Services.Models.Driver
{
    public class DriverLicenseResponse
    {
        public Guid Id { get; set; }
        public DateTime DateOfIssue { get; set; }
        public string IssuedBy { get; set; } = string.Empty;
        public Guid? LicenseImageFileId { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
        public Guid DriverId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
