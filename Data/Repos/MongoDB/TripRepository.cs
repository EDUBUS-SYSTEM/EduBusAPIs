using Data.Contexts.MongoDB;
using Data.Models;
using Data.Repos.Interfaces;
using MongoDB.Driver;

namespace Data.Repos.MongoDB
{
	public class TripRepository : MongoRepository<Trip>, ITripRepository
	{
		public TripRepository(IMongoDatabase database) : base(database, "trips")
		{
		}

		public async Task<IEnumerable<Trip>> GetTripsByRouteAsync(Guid routeId)
		{
			var filter = Builders<Trip>.Filter.Eq(t => t.RouteId, routeId) &
						Builders<Trip>.Filter.Eq(t => t.IsDeleted, false);
			return await FindByFilterAsync(filter);
		}

		public async Task<IEnumerable<Trip>> GetTripsByDateAsync(DateTime serviceDate)
		{
			var startOfDay = serviceDate.Date;
			var endOfDay = startOfDay.AddDays(1);

			var filter = Builders<Trip>.Filter.And(
				Builders<Trip>.Filter.Eq(t => t.IsDeleted, false),
				Builders<Trip>.Filter.Gte(t => t.ServiceDate, startOfDay),
				Builders<Trip>.Filter.Lt(t => t.ServiceDate, endOfDay)
			);
			return await FindByFilterAsync(filter);
		}

		public async Task<IEnumerable<Trip>> GetTripsByDateRangeAsync(DateTime startDate, DateTime endDate)
		{
			var filter = Builders<Trip>.Filter.And(
				Builders<Trip>.Filter.Eq(t => t.IsDeleted, false),
				Builders<Trip>.Filter.Gte(t => t.ServiceDate, startDate.Date),
				Builders<Trip>.Filter.Lte(t => t.ServiceDate, endDate.Date)
			);
			return await FindByFilterAsync(filter);
		}

		public async Task<IEnumerable<Trip>> GetTripsByStatusAsync(string status)
		{
			var filter = Builders<Trip>.Filter.Eq(t => t.Status, status) &
						Builders<Trip>.Filter.Eq(t => t.IsDeleted, false);
			return await FindByFilterAsync(filter);
		}

		public async Task<IEnumerable<Trip>> GetTripsByDriverAsync(Guid driverId)
		{
			// This would require joining with driver assignments
			// For now, return empty - this should be handled in the service layer
			return new List<Trip>();
		}

		public async Task<IEnumerable<Trip>> GetUpcomingTripsAsync(DateTime fromDate, int days = 7)
		{
			var endDate = fromDate.AddDays(days);
			return await GetTripsByDateRangeAsync(fromDate, endDate);
		}

		public async Task<IEnumerable<Trip>> GetTripsByVehicleAndDateAsync(Guid vehicleId, DateTime serviceDate)
		{
			var startOfDay = serviceDate.Date;
			var endOfDay = startOfDay.AddDays(1);

			var filter = Builders<Trip>.Filter.And(
				Builders<Trip>.Filter.Eq(t => t.IsDeleted, false),
				Builders<Trip>.Filter.Gte(t => t.ServiceDate, startOfDay),
				Builders<Trip>.Filter.Lt(t => t.ServiceDate, endOfDay)
			);

			// Get all trips for the date, then filter by vehicle through route
			var allTrips = await FindByFilterAsync(filter);
			
			// Filter trips by vehicle through route relationship
			// Note: This requires joining with Route collection to get vehicle assignments
			// For now, we'll return all trips and let the service layer handle vehicle filtering
			return allTrips;
		}

		public async Task<IEnumerable<Trip>> GetTripsByVehicleAndDateRangeAsync(Guid vehicleId, DateTime startDate, DateTime endDate)
		{
			var filter = Builders<Trip>.Filter.And(
				Builders<Trip>.Filter.Eq(t => t.IsDeleted, false),
				Builders<Trip>.Filter.Gte(t => t.ServiceDate, startDate.Date),
				Builders<Trip>.Filter.Lte(t => t.ServiceDate, endDate.Date)
			);

			// Get all trips for the date range, then filter by vehicle through route
			var allTrips = await FindByFilterAsync(filter);
			
			// Filter trips by vehicle through route relationship
			// Note: This requires joining with Route collection to get vehicle assignments
			// For now, we'll return all trips and let the service layer handle vehicle filtering
			return allTrips;
		}
	}
}