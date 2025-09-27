using Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
		Task<IEnumerable<Trip>> GetTripsByDateRangeAsync(DateTime startDate, DateTime endDate);
		Task<IEnumerable<Trip>> GetTripsByStatusAsync(string status);
		Task<IEnumerable<Trip>> GetUpcomingTripsAsync(DateTime fromDate, int days = 7);
		Task<bool> TripExistsAsync(Guid id);
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
	}
}
