using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Models.PickupPoint
{
    public class CreatePickupPointRequestDto
    {
        [Required, EmailAddress, MaxLength(320)]
        public string ParentEmail { get; set; } = "";

        [Required, MinLength(1)]
        public List<Guid> StudentIds { get; set; } = new();

        [Required, MaxLength(500)]
        public string AddressText { get; set; } = "";

        [Range(-90, 90)]
        public double Latitude { get; set; }

        [Range(-180, 180)]
        public double Longitude { get; set; }

        [Range(0.01, 1000)]
        public double DistanceKm { get; set; }

        [Range(1, 1_000_000_000)]
        public decimal UnitPriceVndPerKm { get; set; } = 50_000m;

        [Range(1, 1_000_000_000)]
        public decimal EstimatedPriceVnd { get; set; }

        [MaxLength(500)]
        public string Description { get; set; } = "";

        [MaxLength(500)]
        public string Reason { get; set; } = "";
    }
}
