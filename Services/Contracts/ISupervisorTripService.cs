using Services.Models.Trip;

namespace Services.Contracts
{
    public interface ISupervisorTripService
    {
        Task<IEnumerable<SupervisorTripListItemDto>> GetSupervisorTripsAsync(
            Guid supervisorId,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            string? status = null);

        Task<IEnumerable<SupervisorTripListItemDto>> GetTodayTripsAsync(Guid supervisorId);

        Task<SupervisorTripDetailDto?> GetSupervisorTripDetailAsync(Guid tripId, Guid supervisorId);
    }
}

