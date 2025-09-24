using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data.Models.Enums;

namespace Services.Models.DriverVehicle
{
    public class DriverInfoDto
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
    }
}
