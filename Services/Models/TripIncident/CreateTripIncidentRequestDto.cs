using System.ComponentModel.DataAnnotations;
using Data.Models.Enums;

namespace Services.Models.TripIncident
{
    public sealed class CreateTripIncidentRequestDto
    {
        [Required]
        public TripIncidentReason Reason { get; set; }

        [MaxLength(200)]
        public string? Title { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }
    }
}

