using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.Contracts;
using Services.Models.Route;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Utils;

namespace Services.Implementations
{
	/// <summary>
	/// VRP engine that calls Vietmap VRP API (https://maps.vietmap.vn/api/vrp)
	/// using VRPData and returns RouteSuggestionResponse.
	/// </summary>
	public class VietMapVrpEngine : IVrpEngine
	{
		private readonly HttpClient _httpClient;
		private readonly string _apiKey;
		private readonly VRPSettings _vrpSettings;
		private readonly ILogger<VietMapVrpEngine> _logger;

		// Maps Vietmap job id -> pickup point id + students for that point
		private readonly Dictionary<int, Guid> _jobToPickupPointMap = new();
		private readonly Dictionary<int, List<Data.Models.Student>> _jobToStudentsMap = new();

		public string Name => "VietMap";

		public VietMapVrpEngine(
			HttpClient httpClient,
			IConfiguration configuration,
			IOptions<VRPSettings> vrpSettings,
			ILogger<VietMapVrpEngine> logger)
		{
			_httpClient = httpClient;
			_apiKey = configuration["VietMap:ApiKey"]
				?? throw new InvalidOperationException("VietMap API key not configured");
			_vrpSettings = vrpSettings.Value;
			_logger = logger;
		}

		public async Task<RouteSuggestionResponse> GenerateSuggestionsAsync(VRPData data)
		{
			var requestBody = BuildVietMapVrpRequest(data);
			if (!requestBody.jobs.Any() || !requestBody.vehicles.Any())
			{
				return new RouteSuggestionResponse
				{
					Success = false,
					Message = "No vehicles or jobs to optimize"
				};
			}

			var url = $"https://maps.vietmap.vn/api/vrp?api-version=1.1&apikey={_apiKey}";

			try
			{
				_logger.LogInformation("Calling VietMap VRP API with {VehicleCount} vehicles and {JobCount} jobs",
					requestBody.vehicles.Count, requestBody.jobs.Count);

				// Log the request payload for debugging
				var requestJson = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { WriteIndented = true });
				_logger.LogInformation("VietMap Request Payload: {RequestPayload}", requestJson);

				var httpResponse = await _httpClient.PostAsJsonAsync(url, requestBody);

				// Read response before checking status
				var responseContent = await httpResponse.Content.ReadAsStringAsync();

				if (!httpResponse.IsSuccessStatusCode)
				{
					_logger.LogError("VietMap API returned {StatusCode}: {ResponseContent}",
						httpResponse.StatusCode, responseContent);
					return new RouteSuggestionResponse
					{
						Success = false,
						Message = $"VietMap API error: {httpResponse.StatusCode} - {responseContent}"
					};
				}

				using var doc = JsonDocument.Parse(responseContent);
				var root = doc.RootElement;

				return ConvertFromVietMapResponse(root, data);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error calling VietMap VRP API");
				return new RouteSuggestionResponse
				{
					Success = false,
					Message = $"Error calling VietMap VRP API: {ex.Message}"
				};
			}
		}

		private VietMapVrpRequest BuildVietMapVrpRequest(VRPData data)
		{
			var vehicles = new List<VietMapVehicle>();
			var jobs = new List<VietMapJob>();

			// 1) Vehicles: one Vietmap vehicle per internal vehicle, round-trip from school
			for (int i = 0; i < data.Vehicles.Count; i++)
			{
				var v = data.Vehicles[i];

				vehicles.Add(new VietMapVehicle
				{
					id = i + 1, // 1-based id
					start = new[] { data.SchoolLocation.Longitude, data.SchoolLocation.Latitude },
					end = new[] { data.SchoolLocation.Longitude, data.SchoolLocation.Latitude },
					profile = "car", // or "bike" etc.
					time_window = new[] { 0, _vrpSettings.MaxRouteDurationSeconds },
					capacity = new[] { v.Capacity },
					speed_factor = 1.0 // default
				});
			}

			// 2) Jobs: each pickup point that has students becomes a job
			var pickupPointGroups = data.Students
				.Where(s => s.CurrentPickupPointId.HasValue)
				.GroupBy(s => s.CurrentPickupPointId!.Value)
				.ToList();

			int jobId = 1;
			foreach (var group in pickupPointGroups)
			{
				var pickupPointId = group.Key;
				var pickupPoint = data.PickupPoints.FirstOrDefault(pp => pp.Id == pickupPointId);
				if (pickupPoint == null) continue;

				var studentsForPoint = group.ToList();
				var load = studentsForPoint.Count;

				var job = new VietMapJob
				{
					id = jobId,
					description = pickupPoint.Description ?? "Pickup point",
					// [lon, lat]
					location = new[] { pickupPoint.Geog.X, pickupPoint.Geog.Y },
					service = _vrpSettings.ServiceTimeSeconds, // seconds spent at stop
					priority = 1,
					time_windows = null, // you can add specific windows if needed
					delivery = new[] { load }
				};

				jobs.Add(job);

				// Track mapping from job id to pickup point + students
				_jobToPickupPointMap[jobId] = pickupPointId;
				_jobToStudentsMap[jobId] = studentsForPoint;

				jobId++;
			}

			return new VietMapVrpRequest
			{
				vehicles = vehicles,
				jobs = jobs
			};
		}

		private RouteSuggestionResponse ConvertFromVietMapResponse(JsonElement root, VRPData data)
		{
			var response = new RouteSuggestionResponse
			{
				Success = true,
				Message = "Route suggestions generated successfully using VietMap VRP",
				GeneratedAt = DateTime.UtcNow,
				Routes = new List<RouteSuggestionDto>()
			};

			if (root.TryGetProperty("code", out var codeElem) &&
				codeElem.ValueKind == JsonValueKind.Number &&
				codeElem.GetInt32() != 0)
			{
				response.Success = false;
				response.Message = $"VietMap VRP returned error code {codeElem.GetInt32()}";
				return response;
			}

			if (!root.TryGetProperty("routes", out var routesElem) ||
				routesElem.ValueKind != JsonValueKind.Array)
			{
				response.Success = false;
				response.Message = "VietMap VRP response does not contain 'routes'";
				return response;
			}

			foreach (var routeElem in routesElem.EnumerateArray())
			{
				var routeDto = new RouteSuggestionDto
				{
					PickupPoints = new List<RoutePickupPointInfoDto>(),
					GeneratedAt = DateTime.UtcNow
				};

				int vehicleExternalId = routeElem.TryGetProperty("vehicle", out var vehicleIdElem) &&
										vehicleIdElem.TryGetInt32(out var vid)
					? vid
					: 0;

				double totalDistanceKm = 0;
				int totalStudents = 0;

				// Prefer route-level distance if present
				if (routeElem.TryGetProperty("distance", out var distElem) &&
					distElem.TryGetDouble(out var routeDistMeters))
				{
					totalDistanceKm = routeDistMeters / 1000.0;
				}

				// Parse steps to build pickup sequence and maybe refine distance
				if (routeElem.TryGetProperty("steps", out var stepsElem) &&
					stepsElem.ValueKind == JsonValueKind.Array)
				{
					int sequence = 0;

					foreach (var step in stepsElem.EnumerateArray())
					{
						var type = step.TryGetProperty("type", out var typeElem)
							? typeElem.GetString()
							: null;

						if (string.Equals(type, "job", StringComparison.OrdinalIgnoreCase))
						{
							if (step.TryGetProperty("job", out var jobIdElem) &&
								jobIdElem.TryGetInt32(out var jobId))
							{
								sequence++;

								var pickupInfo = CreatePickupPointInfoFromJob(jobId, sequence, data);
								if (pickupInfo != null)
								{
									routeDto.PickupPoints.Add(pickupInfo.Value.Item1);
									totalStudents += pickupInfo.Value.Item2;
								}
							}
						}

						// If you want, you could also accumulate distance from steps:
						// if (step.TryGetProperty("distance", out var stepDistElem) &&
						//     stepDistElem.TryGetDouble(out var stepDistMeters)) { ... }
					}
				}

				routeDto.TotalDistance = totalDistanceKm;
				routeDto.TotalStudents = totalStudents;
				routeDto.EstimatedCost = (decimal)(totalDistanceKm * 10000 + totalStudents * 5000);

				if (vehicleExternalId > 0 && vehicleExternalId <= data.Vehicles.Count)
				{
					var vehicle = data.Vehicles[vehicleExternalId - 1];
					routeDto.Vehicle = new RouteVehicleInfo
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

				response.Routes.Add(routeDto);
			}

			response.TotalRoutes = response.Routes.Count;
			return response;
		}

		private (RoutePickupPointInfoDto, int)? CreatePickupPointInfoFromJob(
			int jobId,
			int sequence,
			VRPData data)
		{
			if (!_jobToPickupPointMap.TryGetValue(jobId, out var pickupPointId))
				return null;

			var pickupPoint = data.PickupPoints.FirstOrDefault(pp => pp.Id == pickupPointId);
			if (pickupPoint == null)
				return null;

			if (!_jobToStudentsMap.TryGetValue(jobId, out var students))
				students = new List<Data.Models.Student>();

			var lat = pickupPoint.Geog.Y;
			var lng = pickupPoint.Geog.X;

			var dto = new RoutePickupPointInfoDto
			{
				PickupPointId = pickupPoint.Id,
				Description = pickupPoint.Description,
				Address = pickupPoint.Location,
				Latitude = lat,
				Longitude = lng,
				SequenceOrder = sequence,
				StudentCount = students.Count,
				Students = students.Select(s => new RouteStudentInfo
				{
					Id = s.Id,
					FirstName = s.FirstName,
					LastName = s.LastName,
					ParentEmail = s.ParentEmail
				}).ToList()
			};

			return (dto, students.Count);
		}

		#region Vietmap VRP DTOs

		private class VietMapVrpRequest
		{
			public List<VietMapVehicle> vehicles { get; set; } = new();
			public List<VietMapJob> jobs { get; set; } = new();
		}

		private class VietMapVehicle
		{
			public int id { get; set; }
			public double[] start { get; set; } = Array.Empty<double>();   // [lon, lat]
			public double[] end { get; set; } = Array.Empty<double>();     // [lon, lat]
			public string profile { get; set; } = "car";
			public int[]? time_window { get; set; }
			public int[] capacity { get; set; } = Array.Empty<int>();
			[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
			public int[]? skills { get; set; }
			[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
			public List<VietMapBreak>? breaks { get; set; }
			public double speed_factor { get; set; } = 1.0;
		}

		private class VietMapBreak
		{
			public int id { get; set; }
			[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
			public int[][] time_windows { get; set; } 
			public int service { get; set; }
		}

		private class VietMapJob
		{
			public int id { get; set; }
			public string description { get; set; } = string.Empty;
			public double[] location { get; set; } = Array.Empty<double>(); // [lon, lat]
			public int service { get; set; } // seconds
			public int[] delivery { get; set; } = Array.Empty<int>();
			public int priority { get; set; } = 1;
			[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
			public int[][]? time_windows { get; set; }
			[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
			public int[]? skills { get; set; }
		}

		#endregion
	}
}