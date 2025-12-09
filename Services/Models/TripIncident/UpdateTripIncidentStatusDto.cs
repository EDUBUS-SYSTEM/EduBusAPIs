using System.ComponentModel.DataAnnotations;
using Data.Models.Enums;

namespace Services.Models.TripIncident
{
    public sealed class UpdateTripIncidentStatusDto
    {
        [Required]
        public TripIncidentStatus Status { get; set; }

        [MaxLength(2000)]
        public string? AdminNote { get; set; }
    }
}

