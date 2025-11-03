using Data.Models;
using Services.Models.Trip;

namespace Services.Contracts
{
	public interface ITripService
	{
		Task<IEnumerable<Trip>> GetAllTripsAsync();
		Task<Trip?> GetTripByIdAsync(Guid id);
		Task<Trip> CreateTripAsync(Trip trip);
		Task<Trip?> UpdateTripAsync(Trip trip);
		Task<Trip?> DeleteTripAsync(Guid id);
		Task<IEnumerable<Trip>> GetTripsByRouteAsync(Guid routeId);
		Task<IEnumerable<Trip>> GetTripsByDateAsync(DateTime serviceDate);
		Task<IEnumerable<Trip>> GetUpcomingTripsAsync(DateTime fromDate, int days = 7);
		Task<IEnumerable<Trip>> GenerateTripsFromScheduleAsync(Guid scheduleId, DateTime startDate, DateTime endDate);
		Task<IEnumerable<Trip>> QueryTripsAsync(
			Guid? routeId,
			DateTime? serviceDate,
			DateTime? startDate,
			DateTime? endDate,
			string? status,
			int page,
			int perPage,
			string sortBy,
			string sortOrder);

		Task<bool> UpdateTripStatusAsync(Guid tripId, string newStatus, string? reason = null);
		Task<bool> UpdateAttendanceAsync(Guid tripId, Guid stopId, Guid studentId, string state);
		Task<bool> CascadeDeactivateTripsByRouteAsync(Guid routeId);
		Task<IEnumerable<Trip>> RegenerateTripsForDateAsync(Guid scheduleId, DateTime date);
		Task<IEnumerable<Trip>> GetDriverScheduleByDateAsync(Guid driverId, DateTime serviceDate);
		Task<IEnumerable<Trip>> GetDriverScheduleByRangeAsync(Guid driverId, DateTime startDate, DateTime endDate);
		Task<IEnumerable<Trip>> GetDriverUpcomingScheduleAsync(Guid driverId, int days = 7);
		Task<DriverScheduleSummary> GetDriverScheduleSummaryAsync(Guid driverId, DateTime startDate, DateTime endDate);
	}
}
