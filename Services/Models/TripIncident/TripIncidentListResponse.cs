using Services.Models.Common;

namespace Services.Models.TripIncident
{
    public sealed class TripIncidentListResponse
    {
        public List<TripIncidentListItemDto> Data { get; init; } = new();
        public PaginationInfo Pagination { get; init; } = new();
    }
}

