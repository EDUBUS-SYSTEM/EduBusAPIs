using Data.Models.Enums;

namespace Services.Models.TripIncident
{
    public sealed class TripIncidentListItemDto
    {
        public Guid Id { get; init; }
        public Guid TripId { get; init; }
        public TripIncidentReason Reason { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? Description { get; init; }
        public TripIncidentStatus Status { get; init; }
        public DateTime CreatedAt { get; init; }
        public string RouteName { get; init; } = string.Empty;
        public string VehiclePlate { get; init; } = string.Empty;
        public DateTime ServiceDate { get; init; }
    }
}

