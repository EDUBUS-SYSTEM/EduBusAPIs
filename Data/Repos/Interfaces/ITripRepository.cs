using Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repos.Interfaces
{
	public interface ITripRepository : IMongoRepository<Trip>
	{
		Task<IEnumerable<Trip>> GetTripsByRouteAsync(Guid routeId);
		Task<IEnumerable<Trip>> GetTripsByDateAsync(DateTime serviceDate);
		Task<IEnumerable<Trip>> GetTripsByDateRangeAsync(DateTime startDate, DateTime endDate);
		Task<IEnumerable<Trip>> GetTripsByStatusAsync(string status);
		Task<IEnumerable<Trip>> GetTripsByDriverAsync(Guid driverId);
		Task<IEnumerable<Trip>> GetTripsByDriverAndDateAsync(Guid driverId, DateTime serviceDate);
		Task<IEnumerable<Trip>> GetUpcomingTripsAsync(DateTime fromDate, int days = 7);
		
		Task<IEnumerable<Trip>> GetTripsByVehicleAndDateAsync(Guid vehicleId, DateTime serviceDate);
		Task<IEnumerable<Trip>> GetTripsByVehicleAndDateRangeAsync(Guid vehicleId, DateTime startDate, DateTime endDate);
		Task<IEnumerable<Trip>> GetTripsByStudentAndDateRangeAsync(Guid studentId, DateTime startDate, DateTime endDate);
		Task<bool> StudentHasTripsBetweenDatesAsync(Guid studentId, DateTime startDate, DateTime endDate);
	}
}
