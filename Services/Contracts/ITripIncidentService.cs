using Data.Models.Enums;
using Services.Models.TripIncident;

namespace Services.Contracts
{
    public interface ITripIncidentService
    {
        Task<TripIncidentResponseDto> CreateAsync(Guid tripId, CreateTripIncidentRequestDto request, Guid supervisorId);
        Task<TripIncidentListResponse> GetByTripAsync(Guid tripId, Guid requesterId, bool isAdmin, int page, int perPage);
        Task<TripIncidentListResponse> GetAllAsync(Guid? tripId, Guid? supervisorId, TripIncidentStatus? status, int page, int perPage);
        Task<TripIncidentResponseDto?> GetByIdAsync(Guid incidentId, Guid requesterId, bool isAdmin);
        Task<TripIncidentResponseDto> UpdateStatusAsync(Guid incidentId, UpdateTripIncidentStatusDto request, Guid adminId);
    }
}

