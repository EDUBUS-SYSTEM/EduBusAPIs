using Google.OrTools.ConstraintSolver;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.Contracts;
using Services.Models.Route;
using Utils;

namespace Services.Implementations
{
	public class OrToolsVrpEngine : IVrpEngine
	{
		private readonly VRPSettings _vrpSettings;
		private readonly ILogger<OrToolsVrpEngine> _logger;

		public string Name => "OrTools";

		public OrToolsVrpEngine(
			IOptions<VRPSettings> vrpSettings,
			ILogger<OrToolsVrpEngine> logger)
		{
			_vrpSettings = vrpSettings.Value;
			_logger = logger;
		}

		public Task<RouteSuggestionResponse> GenerateSuggestionsAsync(VRPData data)
		{
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();

			var solution = SolveVRPWithORTools(data);

			stopwatch.Stop();
			_logger.LogInformation("OR-Tools VRP solved in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

			var response = ConvertSolutionToResponse(solution, data);
			response.GeneratedAt = DateTime.UtcNow;

			return Task.FromResult(response);
		}

		private VRPSolution SolveVRPWithORTools(VRPData data)
		{
			try
			{
				int numVehicles = data.Vehicles.Count;
				int depot = 0;
				int numNodes = data.PickupPoints.Count + 1; // +1 for depot

				var manager = new RoutingIndexManager(numNodes, numVehicles, depot);
				var routing = new RoutingModel(manager);

				var distanceCallback = new DistanceCallback(data);
				var transitCallbackIndex = routing.RegisterTransitCallback(distanceCallback.Call);

				var demandCallback = new DemandCallback(data);
				var demandCallbackIndex = routing.RegisterUnaryTransitCallback(demandCallback.Call);

				routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);

				var vehicleCapacities = data.Vehicles.Select(v => (long)v.Capacity).ToArray();
				routing.AddDimensionWithVehicleCapacity(
					demandCallbackIndex,
					0,
					vehicleCapacities,
					true,
					"Capacity"
				);

				if (_vrpSettings.UseTimeWindows)
				{
					AddTimeWindowConstraints(routing, manager, data);
				}

				var searchParameters = operations_research_constraint_solver.DefaultRoutingSearchParameters();
				searchParameters.FirstSolutionStrategy = GetOptimizationStrategy(_vrpSettings.OptimizationType);
				searchParameters.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch;
				searchParameters.TimeLimit = new Google.Protobuf.WellKnownTypes.Duration
				{
					Seconds = _vrpSettings.DefaultTimeLimitSeconds
				};

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
			var timeCallbackIndex = routing.RegisterTransitCallback((fromIndex, toIndex) =>
			{
				if (fromIndex == toIndex) return 0;

				var fromLocation = GetLocationFromIndex(fromIndex, data);
				var toLocation = GetLocationFromIndex(toIndex, data);
				var distance = CalculateHaversineDistance(fromLocation, toLocation);

				var travelTimeSeconds = (long)(distance / _vrpSettings.AverageSpeedKmh * 3600);

				var toNode = manager.IndexToNode(toIndex);
				var serviceTime = toNode == 0 ? 0 : _vrpSettings.ServiceTimeSeconds;

				return travelTimeSeconds + serviceTime;
			});

			routing.AddDimension(
				timeCallbackIndex,
				_vrpSettings.SlackTimeSeconds,
				_vrpSettings.MaxRouteDurationSeconds,
				false,
				"Time"
			);
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
				Message = solution.IsSuccess
					? "Route suggestions generated successfully using OR-Tools VRP"
					: "Failed to generate optimal routes",
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
				if (route.Any(node => node != 0))
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
			long index = solution.Model!.Start(vehicleIndex);

			while (!solution.Model.IsEnd(index))
			{
				var nodeIndex = solution.Manager!.IndexToNode(index);
				route.Add((int)nodeIndex);

				index = solution.Assignment!.Value(solution.Model.NextVar(index));
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

			var previousLocation = data.SchoolLocation;

			for (int i = 0; i < route.Count; i++)
			{
				var nodeIndex = route[i];
				if (nodeIndex == 0) continue;

				var pickupPointIndex = nodeIndex - 1;
				if (pickupPointIndex >= 0 && pickupPointIndex < data.PickupPoints.Count)
				{
					var pickupPoint = data.PickupPoints[pickupPointIndex];
					var students = data.Students.Where(s => s.CurrentPickupPointId == pickupPoint.Id).ToList();

					var coordinates = (lat: pickupPoint.Geog.Y, lng: pickupPoint.Geog.X);
					var currentLocation = new Location { Latitude = coordinates.lat, Longitude = coordinates.lng };

					var distance = CalculateHaversineDistance(previousLocation, currentLocation);

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

					previousLocation = currentLocation;
				}
			}

			routeSuggestion.PickupPoints = pickupPoints;
			routeSuggestion.TotalStudents = totalStudents;
			routeSuggestion.TotalDistance = totalDistance;
			routeSuggestion.EstimatedCost = (decimal)(totalDistance * 10000 + totalStudents * 5000);

			if (vehicleIndex < data.Vehicles.Count)
			{
				var vehicle = data.Vehicles[vehicleIndex];
				routeSuggestion.Vehicle = new RouteVehicleInfo
				{
					VehicleId = vehicle.Id,
					LicensePlate = SecurityHelper.DecryptFromBytes(vehicle.HashedLicensePlate),
					Capacity = vehicle.Capacity,
					AssignedStudents = totalStudents,
					UtilizationPercentage = vehicle.Capacity > 0
						? (double)totalStudents / vehicle.Capacity * 100
						: 0
				};
			}

			return routeSuggestion;
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

		private double CalculateHaversineDistance(Location from, Location to)
		{
			const double R = 6371;
			var dLat = ToRadians(to.Latitude - from.Latitude);
			var dLon = ToRadians(to.Longitude - from.Longitude);
			var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
					Math.Cos(ToRadians(from.Latitude)) * Math.Cos(ToRadians(to.Latitude)) *
					Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
			var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
			return R * c;
		}

		private double ToRadians(double degrees) => degrees * (Math.PI / 180);
	}
}