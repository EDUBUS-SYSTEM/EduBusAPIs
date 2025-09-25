// 3. Service Implementation
using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using Google.OrTools.ConstraintSolver;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTopologySuite.Operation.Distance;
using Services.Contracts;
using Services.Models.Route;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public RouteSuggestionService(
            IMongoRepository<Route> routeRepository,
            ISqlRepository<PickupPoint> pickupPointRepository,
            ISqlRepository<Student> studentRepository,
            ISqlRepository<Vehicle> vehicleRepository,
            IOptions<VRPSettings> vrpSettings,
            ILogger<RouteSuggestionService> logger)
        {
            _routeRepository = routeRepository;
            _pickupPointRepository = pickupPointRepository;
            _studentRepository = studentRepository;
            _vehicleRepository = vehicleRepository;
            _vrpSettings = vrpSettings.Value;
            _logger = logger;
        }

        public async Task<RouteSuggestionResponse> GenerateRouteSuggestionsAsync()
        {
            try
            {
                _logger.LogInformation("Generating route suggestions using OR-Tools VRP");

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

                // Solve VRP using OR-Tools
                var solution = await SolveVRPWithORToolsAsync(vrpData);

                stopwatch.Stop();

                // Convert solution to response
                var response = ConvertSolutionToResponse(solution, vrpData);
                response.GeneratedAt = DateTime.UtcNow;

                _logger.LogInformation("OR-Tools VRP solved in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating route suggestions with OR-Tools");
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

        private async Task<VRPSolution> SolveVRPWithORToolsAsync(VRPData data)
        {
            try
            {
                int numVehicles = data.Vehicles.Count;
                int depot = 0;
                int numNodes = data.PickupPoints.Count + 1; // +1 for depot

                // Create OR-Tools routing model
                var manager = new RoutingIndexManager(numNodes, numVehicles, depot);
                var routing = new RoutingModel(manager);

                // Add distance callback (only distance, no time)
                var distanceCallback = new DistanceCallback(data);
                var transitCallbackIndex = routing.RegisterTransitCallback(distanceCallback.Call);

                // Add demand callback for CVRP
                var demandCallback = new DemandCallback(data);
                var demandCallbackIndex = routing.RegisterUnaryTransitCallback(demandCallback.Call);

                // Set arc cost evaluator (distance-based)
                routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);

                // Add capacity constraints
                var vehicleCapacities = data.Vehicles.Select(v => (long)v.Capacity).ToArray();
                routing.AddDimensionWithVehicleCapacity(
                    demandCallbackIndex,
                    0, // null capacity slack
                    vehicleCapacities,
                    true, // start cumul to zero
                    "Capacity"
                );

                // Add time windows if configured
                if (_vrpSettings.UseTimeWindows)
                {
                    AddTimeWindowConstraints(routing, manager, data);
                }

                // Search parameters from configuration
                var searchParameters = operations_research_constraint_solver.DefaultRoutingSearchParameters();
                searchParameters.FirstSolutionStrategy = GetOptimizationStrategy(_vrpSettings.OptimizationType);
                searchParameters.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch;
                searchParameters.TimeLimit = new Google.Protobuf.WellKnownTypes.Duration
                {
                    Seconds = _vrpSettings.DefaultTimeLimitSeconds
                };

                // Solve
                var assignment = routing.SolveWithParameters(searchParameters);

                return new VRPSolution
                {
                    IsSuccess = assignment != null,
                    Model = routing,
                    Manager = manager,
                    Assignment = assignment,
                    Data = data
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error solving VRP with OR-Tools");
                return new VRPSolution
                {
                    IsSuccess = false,
                    Data = data
                };
            }
        }

        private void AddTimeWindowConstraints(RoutingModel routing, RoutingIndexManager manager, VRPData data)
        {
            // Create time callback for service time
            var timeCallbackIndex = routing.RegisterTransitCallback((fromIndex, toIndex) => {
                if (fromIndex == toIndex) return 0;

                // Calculate travel time from distance
                var fromLocation = GetLocationFromIndex(fromIndex, data);
                var toLocation = GetLocationFromIndex(toIndex, data);
                var distance = CalculateHaversineDistance(fromLocation, toLocation);

                // Convert distance to travel time using configured average speed
                var travelTimeSeconds = (long)(distance / _vrpSettings.AverageSpeedKmh * 3600); // km/h to seconds

                // Add service time if arriving at pickup point
                var toNode = manager.IndexToNode(toIndex);
                var serviceTime = toNode == 0 ? 0 : _vrpSettings.ServiceTimeSeconds;

                return travelTimeSeconds + serviceTime;
            });

            // Add time dimension
            routing.AddDimension(
                timeCallbackIndex,
                _vrpSettings.SlackTimeSeconds, // Use configured slack time
                _vrpSettings.MaxRouteDurationSeconds, // Use configured max route duration
                false, // start cumul to zero
                "Time"
            );

            var timeDimension = routing.GetMutableDimension("Time");

            // Set time windows for pickup points (6:30 AM - 7:45 AM)
            //for (int i = 1; i < data.PickupPoints.Count + 1; i++) // Skip depot (index 0)
            //{
            //    var index = manager.NodeToIndex(i);
            //    timeDimension.CumulVar(index).SetRange(
            //        TimeToSeconds(6, 30), // 6:30 AM
            //        TimeToSeconds(7, 45)  // 7:45 AM
            //    );
            //}

            //// School arrival deadline (8:00 AM)
            //var schoolIndex = manager.NodeToIndex(0);
            //timeDimension.CumulVar(schoolIndex).SetMax(TimeToSeconds(8, 0));
        }


        private FirstSolutionStrategy.Types.Value GetOptimizationStrategy(string optimizationType)
        {
            return optimizationType.ToLower() switch
            {
                "distance" => FirstSolutionStrategy.Types.Value.PathCheapestArc,
                "time" => FirstSolutionStrategy.Types.Value.PathMostConstrainedArc,
                "cost" => FirstSolutionStrategy.Types.Value.Savings,
                _ => FirstSolutionStrategy.Types.Value.PathCheapestArc
            };
        }

        private RouteSuggestionResponse ConvertSolutionToResponse(VRPSolution solution, VRPData data)
        {
            var response = new RouteSuggestionResponse
            {
                Success = solution.IsSuccess,
                Message = solution.IsSuccess ?
                    "Route suggestions generated successfully using OR-Tools VRP" :
                    "Failed to generate optimal routes",
                GeneratedAt = DateTime.UtcNow
            };

            if (!solution.IsSuccess || solution.Assignment == null)
            {
                return response;
            }

            var routes = new List<RouteSuggestionDto>();

            for (int vehicleIndex = 0; vehicleIndex < data.Vehicles.Count; vehicleIndex++)
            {
                var route = GetRouteForVehicle(solution, vehicleIndex);
                if (route.Any(node => node != 0)) // Only if route has pickup points
                {
                    var routeSuggestion = CreateRouteSuggestion(route, vehicleIndex, data);
                    routes.Add(routeSuggestion);
                }
            }

            response.Routes = routes;
            response.TotalRoutes = routes.Count;

            return response;
        }

        private List<int> GetRouteForVehicle(VRPSolution solution, int vehicleIndex)
        {
            var route = new List<int>();

            // Get starting index for this vehicle
            long index = solution.Model.Start(vehicleIndex);

            // Traverse the route
            while (!solution.Model.IsEnd(index))
            {
                var nodeIndex = solution.Manager.IndexToNode(index);
                route.Add((int)nodeIndex);

                // Get next index
                index = solution.Assignment.Value(solution.Model.NextVar(index));
            }

            return route;
        }

        private RouteSuggestionDto CreateRouteSuggestion(List<int> route, int vehicleIndex, VRPData data)
        {
            var routeSuggestion = new RouteSuggestionDto
            {
            
                GeneratedAt = DateTime.UtcNow
            };

            var pickupPoints = new List<RoutePickupPointInfoDto>();
            var totalStudents = 0;
            var totalDistance = 0.0;

            Location previousLocation = data.SchoolLocation;

            for (int i = 0; i < route.Count; i++)
            {
                var nodeIndex = route[i];
                if (nodeIndex == 0) continue; // Skip depot

                var pickupPointIndex = nodeIndex - 1;
                if (pickupPointIndex >= 0 && pickupPointIndex < data.PickupPoints.Count)
                {
                    var pickupPoint = data.PickupPoints[pickupPointIndex];
                    var students = data.Students.Where(s => s.CurrentPickupPointId == pickupPoint.Id).ToList();

                    // Get coordinates from NetTopologySuite Point
                    var coordinates = GetCoordinatesFromPickupPoint(pickupPoint);
                    var currentLocation = new Location { Latitude = coordinates.lat, Longitude = coordinates.lng };

                    // Calculate ACTUAL travel time
                    var distance = CalculateHaversineDistance(previousLocation, currentLocation);
                    var travelTimeSeconds = (long)(distance / _vrpSettings.AverageSpeedKmh * 3600);
                    var serviceTimeSeconds = _vrpSettings.ServiceTimeSeconds;

                    var pickupPointInfo = new RoutePickupPointInfoDto
                    {
                        PickupPointId = pickupPoint.Id,
                        Description = pickupPoint.Description,
                        Address = pickupPoint.Location,
                        Latitude = coordinates.lat,
                        Longitude = coordinates.lng,
                        SequenceOrder = pickupPoints.Count + 1,
                        StudentCount = students.Count,
                        Students = students.Select(s => new RouteStudentInfo
                        {
                            Id = s.Id,
                            FirstName = s.FirstName,
                            LastName = s.LastName,
                            ParentEmail = s.ParentEmail
                        }).ToList()
                    };

                    pickupPoints.Add(pickupPointInfo);
                    totalStudents += students.Count;
                    totalDistance += distance;

                    // Update previous location
                    previousLocation = currentLocation;

                }
            }

            routeSuggestion.PickupPoints = pickupPoints;
            routeSuggestion.TotalStudents = totalStudents;
            routeSuggestion.TotalDistance = totalDistance;
            routeSuggestion.EstimatedCost = CalculateEstimatedCost(totalDistance, totalStudents);

            // Assign vehicle
            if (vehicleIndex < data.Vehicles.Count)
            {
                var vehicle = data.Vehicles[vehicleIndex];
                routeSuggestion.Vehicle = new RouteVehicleInfo
                {
                    VehicleId = vehicle.Id,
                    LicensePlate = "***", // Hashed in database
                    Capacity = vehicle.Capacity,
                    AssignedStudents = totalStudents,
                    UtilizationPercentage = vehicle.Capacity > 0 ? (double)totalStudents / vehicle.Capacity * 100 : 0
                };
            }

            return routeSuggestion;
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

        private decimal CalculateEstimatedCost(double distance, int students)
        {
            return (decimal)(distance * 10000 + students * 5000); // VND
        }

        private (double lat, double lng) GetCoordinatesFromPickupPoint(PickupPoint pickupPoint)
        {
            return (pickupPoint.Geog.Y, pickupPoint.Geog.X);
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
        private Location GetLocationFromIndex(long index, VRPData data)
        {
            if (index == 0) return data.SchoolLocation; 

            var pickupPointIndex = (int)index - 1;
            if (pickupPointIndex >= 0 && pickupPointIndex < data.PickupPoints.Count)
            {
                var pickupPoint = data.PickupPoints[pickupPointIndex];
                return new Location
                {
                    Latitude = pickupPoint.Geog.Y,
                    Longitude = pickupPoint.Geog.X
                };
            }

            return data.SchoolLocation;
        }
        private double ToRadians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }

        private int TimeToSeconds(int hours, int minutes)
        {
            return hours * 3600 + minutes * 60;
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