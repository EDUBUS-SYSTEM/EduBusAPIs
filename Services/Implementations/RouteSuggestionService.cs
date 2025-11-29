using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using Google.OrTools.ConstraintSolver;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.Contracts;
using Services.Models.Route;

namespace Services.Implementations
{
    public class RouteSuggestionService : IRouteSuggestionService
    {
        private readonly IMongoRepository<Route> _routeRepository;
        private readonly ISqlRepository<PickupPoint> _pickupPointRepository;
        private readonly ISqlRepository<Student> _studentRepository;
        private readonly ISqlRepository<Vehicle> _vehicleRepository;
        private readonly VRPSettings _vrpSettings;
        private readonly ILogger<RouteSuggestionService> _logger;
		private readonly IVrpEngine _vrpEngine;

		public RouteSuggestionService(
	        IMongoRepository<Route> routeRepository,
	        ISqlRepository<PickupPoint> pickupPointRepository,
	        ISqlRepository<Student> studentRepository,
	        ISqlRepository<Vehicle> vehicleRepository,
	        IOptions<VRPSettings> vrpSettings,
	        IVrpEngine vrpEngine,
	        ILogger<RouteSuggestionService> logger)
		{
			_routeRepository = routeRepository;
			_pickupPointRepository = pickupPointRepository;
			_studentRepository = studentRepository;
			_vehicleRepository = vehicleRepository;
			_vrpSettings = vrpSettings.Value;
			_vrpEngine = vrpEngine;        
			_logger = logger;
		}

		public async Task<RouteSuggestionResponse> GenerateRouteSuggestionsAsync()
		{
			try
			{
				_logger.LogInformation("Generating route suggestions using VRP engine");

				var stopwatch = System.Diagnostics.Stopwatch.StartNew();

				// Get data for VRP
				var vrpData = await PrepareVRPDataAsync();
				if (!vrpData.IsValid)
				{
					return new RouteSuggestionResponse
					{
						Success = false,
						Message = vrpData.ErrorMessage
					};
				}

				// Use injected engine
				var response = await _vrpEngine.GenerateSuggestionsAsync(vrpData);
				response.GeneratedAt = DateTime.UtcNow;

				stopwatch.Stop();
				_logger.LogInformation("VRP engine {Engine} solved in {ElapsedMs}ms",
					_vrpEngine.Name, stopwatch.ElapsedMilliseconds);

				return response;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error generating route suggestions");
				return new RouteSuggestionResponse
				{
					Success = false,
					Message = "An error occurred while generating route suggestions"
				};
			}
		}

		public async Task<RouteSuggestionResponse> OptimizeExistingRouteAsync(Guid routeId)
        {
            try
            {
                var route = await _routeRepository.FindAsync(routeId);
                if (route == null)
                {
                    return new RouteSuggestionResponse
                    {
                        Success = false,
                        Message = "Route not found"
                    };
                }

                return await GenerateRouteSuggestionsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error optimizing existing route {RouteId}", routeId);
                return new RouteSuggestionResponse
                {
                    Success = false,
                    Message = "An error occurred while optimizing the route"
                };
            }
        }

        #region Private Helper Methods

        private async Task<VRPData> PrepareVRPDataAsync()
        {
            try
            {
                // Get all active students with pickup points
                var students = await GetActiveStudentsWithPickupPointsAsync();
                if (!students.Any())
                {
                    return new VRPData { IsValid = false, ErrorMessage = "No active students found" };
                }

                // Get all active pickup points
                var pickupPoints = await GetAllActivePickupPointsAsync();
                if (!pickupPoints.Any())
                {
                    return new VRPData { IsValid = false, ErrorMessage = "No pickup points found" };
                }

                // Get all available vehicles
                var vehicles = await GetAllAvailableVehiclesAsync();
                if (!vehicles.Any())
                {
                    return new VRPData { IsValid = false, ErrorMessage = "No available vehicles found" };
                }

                var vrpData = new VRPData
                {
                    IsValid = true,
                    Students = students,
                    PickupPoints = pickupPoints,
                    Vehicles = vehicles,
                    SchoolLocation = new Location
                    {
                        Latitude = _vrpSettings.SchoolLatitude,
                        Longitude = _vrpSettings.SchoolLongitude
                    }
                };

                return ValidateCapacityConstraints(vrpData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing VRP data");
                return new VRPData { IsValid = false, ErrorMessage = "Error preparing data" };
            }
        }

        private VRPData ValidateCapacityConstraints(VRPData data)
        {
            var totalStudents = data.Students.Count;
            var totalCapacity = data.Vehicles.Sum(v => v.Capacity);

            if (totalStudents > totalCapacity)
            {
                return new VRPData
                {
                    IsValid = false,
                    ErrorMessage = $"Insufficient vehicle capacity. Required: {totalStudents} students, Available: {totalCapacity} seats. Need {totalStudents - totalCapacity} more seats or {Math.Ceiling((double)(totalStudents - totalCapacity) / Math.Max(data.Vehicles.Max(v => v.Capacity), 1))} additional vehicles."
                };
            }

            // Check if we have at least one vehicle that can accommodate the largest pickup point
            var maxStudentsPerPickupPoint = data.Students
                .GroupBy(s => s.CurrentPickupPointId)
                .Max(g => g.Count());

            var maxVehicleCapacity = data.Vehicles.Max(v => v.Capacity);
            if (maxStudentsPerPickupPoint > maxVehicleCapacity)
            {
                return new VRPData
                {
                    IsValid = false,
                    ErrorMessage = $"Largest pickup point has {maxStudentsPerPickupPoint} students, but maximum vehicle capacity is {maxVehicleCapacity}. Cannot accommodate all students from this pickup point."
                };
            }

            return data; // Valid
        }

        // Helper methods for data retrieval
        private async Task<List<Student>> GetActiveStudentsWithPickupPointsAsync()
        {
            return (await _studentRepository.FindByConditionAsync(
                s => 
                     s.CurrentPickupPointId.HasValue &&
                     s.Status == StudentStatus.Active,
                s => s.CurrentPickupPoint)).ToList();
        }

        private async Task<List<PickupPoint>> GetAllActivePickupPointsAsync()
        {
            return (await _pickupPointRepository.FindByConditionAsync(
                pp => !pp.IsDeleted)).ToList();
        }

        private async Task<List<Vehicle>> GetAllAvailableVehiclesAsync()
        {
            return (await _vehicleRepository.FindByConditionAsync(v =>
                v.Status == VehicleStatus.Active)).ToList();
        }

        #endregion
    }

    #region OR-Tools Helper Classes

    public class VRPData
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<Student> Students { get; set; } = new();
        public List<PickupPoint> PickupPoints { get; set; } = new();
        public List<Vehicle> Vehicles { get; set; } = new();
        public Location SchoolLocation { get; set; } = new();
    }

    public class Location
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class VRPSolution
    {
        public bool IsSuccess { get; set; }
        public RoutingModel? Model { get; set; }
        public RoutingIndexManager? Manager { get; set; }
        public Assignment? Assignment { get; set; }
        public VRPData? Data { get; set; }
    }

    public class DistanceCallback
    {
        private readonly VRPData _data;

        public DistanceCallback(VRPData data)
        {
            _data = data;
        }

        public long Call(long fromIndex, long toIndex)
        {
            if (fromIndex == toIndex) return 0;

            var fromLocation = GetLocationFromIndex(fromIndex);
            var toLocation = GetLocationFromIndex(toIndex);

            // Only distance, no time component
            return (long)(CalculateHaversineDistance(fromLocation, toLocation) * 1000); // meters
        }

        private Location GetLocationFromIndex(long index)
        {
            if (index == 0) return _data.SchoolLocation; // Depot

            var pickupPointIndex = (int)index - 1;
            if (pickupPointIndex >= 0 && pickupPointIndex < _data.PickupPoints.Count)
            {
                var pickupPoint = _data.PickupPoints[pickupPointIndex];
                return new Location
                {
                    Latitude = pickupPoint.Geog.Y,
                    Longitude = pickupPoint.Geog.X
                };
            }

            return _data.SchoolLocation;
        }

        private double CalculateHaversineDistance(Location from, Location to)
        {
            const double R = 6371; // Earth's radius in kilometers
            var dLat = ToRadians(to.Latitude - from.Latitude);
            var dLon = ToRadians(to.Longitude - from.Longitude);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(from.Latitude)) * Math.Cos(ToRadians(to.Latitude)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }
    }

    public class DemandCallback
    {
        private readonly VRPData _data;

        public DemandCallback(VRPData data)
        {
            _data = data;
        }

        public long Call(long fromIndex)
        {
            if (fromIndex == 0) return 0; // Depot has no demand

            var pickupPointIndex = (int)fromIndex - 1;
            if (pickupPointIndex >= 0 && pickupPointIndex < _data.PickupPoints.Count)
            {
                var pickupPoint = _data.PickupPoints[pickupPointIndex];
                return _data.Students.Count(s => s.CurrentPickupPointId == pickupPoint.Id);
            }

            return 0;
        }
    }

    #endregion
}