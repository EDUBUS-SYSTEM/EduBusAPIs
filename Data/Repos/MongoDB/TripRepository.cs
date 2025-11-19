using Data.Contexts.MongoDB;
using Data.Models;
using Data.Repos.Interfaces;
using MongoDB.Driver;
using System.Linq;

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
			var filter = Builders<Trip>.Filter.And(
				Builders<Trip>.Filter.Eq(t => t.IsDeleted, false),
				Builders<Trip>.Filter.Eq("driver.id", driverId)
			);
			return await FindByFilterAsync(filter);
		}

		public async Task<IEnumerable<Trip>> GetTripsByDriverAndDateAsync(Guid driverId, DateTime serviceDate)
		{
			var startOfDay = serviceDate.Date;
			var endOfDay = startOfDay.AddDays(1);

			var filter = Builders<Trip>.Filter.And(
				Builders<Trip>.Filter.Eq(t => t.IsDeleted, false),
				Builders<Trip>.Filter.Eq("driver.id", driverId),
				Builders<Trip>.Filter.Gte(t => t.ServiceDate, startOfDay),
				Builders<Trip>.Filter.Lt(t => t.ServiceDate, endOfDay)
			);
			return await FindByFilterAsync(filter);
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
				Builders<Trip>.Filter.Eq(t => t.VehicleId, vehicleId),
				Builders<Trip>.Filter.Gte(t => t.ServiceDate, startOfDay),
				Builders<Trip>.Filter.Lt(t => t.ServiceDate, endOfDay)
			);

			return await FindByFilterAsync(filter);
		}

		public async Task<IEnumerable<Trip>> GetTripsByVehicleAndDateRangeAsync(Guid vehicleId, DateTime startDate, DateTime endDate)
		{
			var filter = Builders<Trip>.Filter.And(
				Builders<Trip>.Filter.Eq(t => t.IsDeleted, false),
				Builders<Trip>.Filter.Eq(t => t.VehicleId, vehicleId),
				Builders<Trip>.Filter.Gte(t => t.ServiceDate, startDate.Date),
				Builders<Trip>.Filter.Lte(t => t.ServiceDate, endDate.Date)
			);

			return await FindByFilterAsync(filter);
		}

		public async Task<IEnumerable<Trip>> GetTripsByStudentAndDateRangeAsync(Guid studentId, DateTime startDate, DateTime endDate)
		{
			var start = startDate.Date;
			var end = endDate.Date;

			var dateFilter = Builders<Trip>.Filter.And(
				Builders<Trip>.Filter.Gte(t => t.ServiceDate, start),
				Builders<Trip>.Filter.Lte(t => t.ServiceDate, end)
			);

			var attendanceFilter = Builders<Trip>.Filter.ElemMatch(
				t => t.Stops,
				stop => stop.Attendance != null &&
						stop.Attendance.Any(a => a.StudentId == studentId)
			);

			var filter = Builders<Trip>.Filter.And(
				Builders<Trip>.Filter.Eq(t => t.IsDeleted, false),
				dateFilter,
				attendanceFilter
			);

			return await FindByFilterAsync(filter);
		}

		public async Task<bool> StudentHasTripsBetweenDatesAsync(Guid studentId, DateTime startDate, DateTime endDate)
		{
			var start = startDate.Date;
			var end = endDate.Date;

			var dateFilter = Builders<Trip>.Filter.And(
				Builders<Trip>.Filter.Gte(t => t.ServiceDate, start),
				Builders<Trip>.Filter.Lte(t => t.ServiceDate, end)
			);

			var attendanceFilter = Builders<Trip>.Filter.ElemMatch(
				t => t.Stops,
				stop => stop.Attendance != null &&
						stop.Attendance.Any(a => a.StudentId == studentId)
			);

			var filter = Builders<Trip>.Filter.And(
				Builders<Trip>.Filter.Eq(t => t.IsDeleted, false),
				dateFilter,
				attendanceFilter
			);

			return await _collection.Find(filter).AnyAsync();
		}
	}
}