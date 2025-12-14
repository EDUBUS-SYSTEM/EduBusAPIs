using Data.Models;
using Data.Repos.Interfaces;
using Microsoft.Extensions.Logging;
using Services.Contracts;
using Services.Models.Jetson;
using Utils;

namespace Services.Implementations
{
	public class JetsonService : IJetsonService
	{
		private readonly IMongoRepository<Route> _routeRepository;
		private readonly IStudentRepository _studentRepository;
		private readonly IFaceEmbeddingRepository _faceEmbeddingRepository;
		private readonly ITripService _tripService;
		private readonly IVehicleRepository _vehicleRepository;
		private readonly IMongoRepository<Trip> _tripRepository;
		private readonly ILogger<JetsonService> _logger;

		public JetsonService(
			IMongoRepository<Route> routeRepository,
			IStudentRepository studentRepository,
			IFaceEmbeddingRepository faceEmbeddingRepository,
			ITripService tripService,
			IVehicleRepository vehicleRepository,
			IMongoRepository<Trip> tripRepository,
			ILogger<JetsonService> logger)
		{
			_routeRepository = routeRepository;
			_studentRepository = studentRepository;
			_faceEmbeddingRepository = faceEmbeddingRepository;
			_tripService = tripService;
			_vehicleRepository = vehicleRepository;
			_tripRepository = tripRepository;
			_logger = logger;
		}

		public async Task<ActiveTripResponse?> GetActiveTripForPlateAsync(string plateNumber)
		{
			// 1. Normalize input plate
			var normalizedInput = NormalizePlate(plateNumber);

			// 2. Find matching vehicle (Decrypting plates)
			// TODO: Cache this if performance becomes an issue (Vehicle list is small usually)
			var vehicles = await _vehicleRepository.FindByConditionAsync(v => !v.IsDeleted && v.Status == Data.Models.Enums.VehicleStatus.Active);
			
			Data.Models.Vehicle? matchedVehicle = null;
			foreach (var v in vehicles)
			{
				try 
				{
					// Decrypt
					var decrypted = SecurityHelper.DecryptFromBytes(v.HashedLicensePlate);
					if (NormalizePlate(decrypted) == normalizedInput)
					{
						matchedVehicle = v;
						break;
					}
				}
				catch 
				{
					// Skip if decryption fails
				}
			}

			if (matchedVehicle == null)
			{
				_logger.LogWarning("No vehicle found for plate {Plate}", plateNumber);
				return null;
			}

			// 3. Find active trip for this vehicle
			// Status: "InProgress"
			var activeTrip = (await _tripRepository.FindByConditionAsync(t => 
				t.VehicleId == matchedVehicle.Id && 
				t.Status == Constants.TripConstants.TripStatus.InProgress
			)).FirstOrDefault();

			if (activeTrip == null)
			{
				_logger.LogInformation("Vehicle {Plate} found but no active trip in progress", plateNumber);
				return null;
			}

			return new ActiveTripResponse
			{
				TripId = activeTrip.Id,
				RouteId = activeTrip.RouteId,
				LicensePlate = plateNumber,
				RouteName = activeTrip.ScheduleSnapshot?.Name ?? "Unknown Route"
			};
		}

		private string NormalizePlate(string plate)
		{
			if (string.IsNullOrEmpty(plate)) return string.Empty;
			return plate.Replace(" ", "").Replace("-", "").Replace(".", "").ToUpperInvariant();
		}

		public async Task<bool> SubmitAttendanceAsync(SubmitAttendanceRequest request)
		{
			try
			{
				var tripRequest = new Models.Trip.FaceRecognitionAttendanceRequest
				{
					PickupPointId = request.PickupPointId,
					StudentId = request.StudentId,
					Similarity = request.Similarity,
					LivenessScore = request.LivenessScore,
					FramesConfirmed = request.FramesConfirmed,
					DeviceId = request.DeviceId,
					RecognizedAt = request.RecognizedAt
				};

				var result = await _tripService.SubmitFaceRecognitionAttendanceAsync(request.TripId, tripRequest);
				
				if (result.success)
				{
					_logger.LogInformation("Successfully submitted attendance for student {StudentId} on trip {TripId}", 
						request.StudentId, request.TripId);
					return true;
				}
				else
				{
					_logger.LogWarning("Failed to submit attendance for student {StudentId}: {Message}", 
						request.StudentId, result.message);
					return false;
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error submitting attendance for student {StudentId}", request.StudentId);
				return false;
			}
		}

		public async Task<JetsonStudentSyncResponse> GetStudentsForSyncAsync(string deviceId, Guid routeId)
		{
			// 1. Get route from MongoDB
			var route = await _routeRepository.FindAsync(routeId);
			if (route == null)
				throw new KeyNotFoundException($"Route {routeId} not found");

			_logger.LogInformation("Jetson device {DeviceId} syncing students for route {RouteId}", deviceId, routeId);

			// 2. Get all pickup points for this route
			var pickupPointIds = route.PickupPoints?.Select(s => s.PickupPointId).ToList() ?? new List<Guid>();

			if (!pickupPointIds.Any())
			{
				_logger.LogWarning("Route {RouteId} has no pickup points", routeId);
				return new JetsonStudentSyncResponse
				{
					RouteId = routeId,
					RouteName = route.RouteName,
					TotalStudents = 0,
					SyncedAt = DateTime.UtcNow,
					Students = new List<JetsonStudentData>()
				};
			}

			// 3. Get all students for these pickup points
			var allStudents = new List<Student>();
			foreach (var pickupPointId in pickupPointIds)
			{
				var students = await _studentRepository.GetStudentsByPickupPointAsync(pickupPointId);
				allStudents.AddRange(students);
			}

			// Remove duplicates (student might be in multiple stops)
			var uniqueStudents = allStudents.DistinctBy(s => s.Id).ToList();

			_logger.LogInformation("Found {Count} unique students for route {RouteId}", uniqueStudents.Count, routeId);

			// 4. Get embeddings for these students
			var studentData = new List<JetsonStudentData>();

			foreach (var student in uniqueStudents)
			{
				var embedding = await _faceEmbeddingRepository.GetByStudentIdAsync(student.Id);
				
				if (embedding == null)
				{
					_logger.LogWarning("Student {StudentId} has no face embedding, skipping", student.Id);
					continue;
				}

				// Deserialize embedding JSON to List<float>
				List<float>? embeddingVector;
				try
				{
					embeddingVector = System.Text.Json.JsonSerializer.Deserialize<List<float>>(embedding.EmbeddingJson);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Failed to deserialize embedding for student {StudentId}", student.Id);
					continue;
				}

				if (embeddingVector == null || embeddingVector.Count != 512)
				{
					_logger.LogWarning("Invalid embedding for student {StudentId}: expected 512-dim, got {Count}", 
						student.Id, embeddingVector?.Count ?? 0);
					continue;
				}

				studentData.Add(new JetsonStudentData
				{
					StudentId = student.Id,
					StudentName = $"{student.FirstName} {student.LastName}",
					PhotoUrl = null, // Image model uses HashedUrl (byte[]), not string URL
					Embedding = embeddingVector,
					ModelVersion = embedding.ModelVersion
				});
			}

			_logger.LogInformation("Successfully synced {Count} students with embeddings for route {RouteId}", 
				studentData.Count, routeId);

			return new JetsonStudentSyncResponse
			{
				RouteId = routeId,
				RouteName = route.RouteName,
				TotalStudents = studentData.Count,
				SyncedAt = DateTime.UtcNow,
				Students = studentData
			};
		}
	}
}
