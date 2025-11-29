using Data.Models;
using Services.Models.Trip;

namespace Services.Contracts
{
	public interface ITripService
	{
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
		Task<bool> UpdateAttendanceAsync(Guid tripId, Guid? stopId, Guid studentId, string state);
		Task<bool> CascadeDeactivateTripsByRouteAsync(Guid routeId);
		Task<IEnumerable<Trip>> RegenerateTripsForDateAsync(Guid scheduleId, DateTime date);
		Task<IEnumerable<Trip>> GetDriverScheduleByDateAsync(Guid driverId, DateTime serviceDate);
		Task<IEnumerable<Trip>> GetDriverScheduleByRangeAsync(Guid driverId, DateTime startDate, DateTime endDate);
		Task<IEnumerable<Trip>> GetDriverUpcomingScheduleAsync(Guid driverId, int days = 7);
		Task<DriverScheduleSummary> GetDriverScheduleSummaryAsync(Guid driverId, DateTime startDate, DateTime endDate);
		
		// Driver-specific trip operations
		Task<IEnumerable<Trip>> GetTripsByDateForDriverAsync(Guid driverId, DateTime? date = null);
		Task<Trip?> GetTripDetailForDriverAsync(Guid tripId, Guid driverId);
		Task<bool> StartTripAsync(Guid tripId, Guid driverId);
		Task<bool> EndTripAsync(Guid tripId, Guid driverId);
		Task<bool> UpdateTripLocationAsync(Guid tripId, Guid driverId, double latitude, double longitude, double? speed = null, double? accuracy = null, bool isMoving = false);
		
		// Admin-specific trip operations
		Task<Trip?> GetTripDetailForAdminAsync(Guid tripId);
		Task<Trip?> GetTripWithStopsAsync(Guid tripId);
		Task<IEnumerable<Trip>> GetTripsByDateWithDetailsAsync(DateTime serviceDate);
		Task<Trip?> GetTripDetailForDriverWithStopsAsync(Guid tripId, Guid driverId);
		Task<object> GenerateAllTripsAutomaticAsync(int daysAhead = 7);
		Task<TripListResponse> QueryTripsWithPaginationAsync(
			Guid? routeId,
			DateTime? serviceDate,
			DateTime? startDate,
			DateTime? endDate,
			string? status,
			int page,
			int perPage,
			string sortBy,
			string sortOrder);
		
		// Parent-specific trip operations
		Task<IEnumerable<Trip>> GetTripsByScheduleForParentAsync(string parentEmail, int days = 7);
		Task<IEnumerable<Trip>> GetTripsByDateForParentAsync(string parentEmail, DateTime? date = null);
		Task<Trip?> GetTripDetailForParentAsync(Guid tripId, string parentEmail);
		Task<Trip.VehicleLocation?> GetTripCurrentLocationAsync(Guid tripId, string parentEmail);
        Task<IEnumerable<Guid>> GetParentsForPickupPointAsync(Guid tripId, Guid pickupPointId);
        Task ConfirmArrivalAtStopAsync(Guid tripId, Guid stopId, Guid driverId);
		Task<Trip?> ArrangeStopSequenceAsync(Guid tripId, Guid driverId, Guid pickupPointId, int newSequenceOrder);
		Task<Trip?> UpdateMultipleStopsSequenceAsync(Guid tripId, Guid driverId, List<(Guid PickupPointId, int SequenceOrder)> stopSequences);
	}
}
