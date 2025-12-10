using Data.Models.Enums;

namespace Services.Models.TripIncident
{
    public sealed class TripIncidentResponseDto
    {
        public Guid Id { get; init; }
        public Guid TripId { get; init; }
        public Guid SupervisorId { get; init; }
        public string SupervisorName { get; init; } = string.Empty;
        public TripIncidentReason Reason { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? Description { get; init; }
        public TripIncidentStatus Status { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
        public DateTime ServiceDate { get; init; }
        public string TripStatus { get; init; } = string.Empty;
        public string RouteName { get; init; } = string.Empty;
        public string VehiclePlate { get; init; } = string.Empty;
        public string? AdminNote { get; init; }
        public Guid? HandledBy { get; init; }
        public DateTime? HandledAt { get; init; }
    }
}

