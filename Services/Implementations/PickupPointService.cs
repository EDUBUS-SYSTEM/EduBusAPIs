using AutoMapper;
using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using Microsoft.EntityFrameworkCore;
using Services.Contracts;
using Services.Models.PickupPoint;

namespace Services.Implementations
{
	public class PickupPointService : IPickupPointService
	{
		private readonly IPickupPointRepository _pickupPointRepository;
		private readonly IStudentRepository _studentRepository;
		private readonly IMongoRepository<Route> _routeRepository;
		private readonly IMapper _mapper;

		public PickupPointService(
			IPickupPointRepository pickupPointRepository,
			IStudentRepository studentRepository,
			IMongoRepository<Route> routeRepository,
			IMapper mapper)
		{
			_pickupPointRepository = pickupPointRepository;
			_studentRepository = studentRepository;
			_routeRepository = routeRepository;
			_mapper = mapper;
		}

		public async Task<PickupPointsResponse> GetUnassignedPickupPointsAsync()
		{
			// Get all active routes to find assigned pickup points
			var activeRoutes = await _routeRepository.FindByConditionAsync(r =>
				!r.IsDeleted && r.IsActive);

			// Get all pickup point IDs that are assigned to routes
			var assignedPickupPointIds = activeRoutes
				.SelectMany(r => r.PickupPoints)
				.Select(pp => pp.PickupPointId)
				.Distinct()
				.ToHashSet();

			// Get all pickup points that are NOT assigned to any route
			var unassignedPickupPoints = await _pickupPointRepository.FindByConditionAsync(pp =>
				!pp.IsDeleted && !assignedPickupPointIds.Contains(pp.Id));

			// Get student counts for each unassigned pickup point
			var pickupPointIds = unassignedPickupPoints.Select(pp => pp.Id).ToList();
			var studentCounts = await _studentRepository.FindByConditionAsync(s =>
				pickupPointIds.Contains(s.CurrentPickupPointId ?? Guid.Empty) &&
				s.Status == StudentStatus.Active &&
				!s.IsDeleted);

			// Group students by pickup point ID and count them
			var studentCountsByPickupPoint = studentCounts
				.Where(s => s.CurrentPickupPointId.HasValue)
				.GroupBy(s => s.CurrentPickupPointId!.Value)
				.ToDictionary(g => g.Key, g => g.Count());

			// Map to DTOs with student counts
			var pickupPointDtos = unassignedPickupPoints.Select(pp => new PickupPointDto
			{
				Id = pp.Id,
				Description = pp.Description,
				Location = pp.Location,
				Latitude = pp.Geog.Y, // NetTopologySuite uses Y for latitude
				Longitude = pp.Geog.X, // NetTopologySuite uses X for longitude
				StudentCount = studentCountsByPickupPoint.GetValueOrDefault(pp.Id, 0),
				CreatedAt = pp.CreatedAt,
				UpdatedAt = pp.UpdatedAt
			}).ToList();

			return new PickupPointsResponse
			{
				PickupPoints = pickupPointDtos,
				TotalCount = pickupPointDtos.Count
			};
		}

		/// <summary>
		/// Admin creates a pickup point directly (auto-approved, no approval workflow needed)
		/// </summary>
		public async Task<AdminCreatePickupPointResponse> AdminCreatePickupPointAsync(
			AdminCreatePickupPointRequest request, Guid adminId)
		{
			if (request == null)
				throw new ArgumentNullException(nameof(request));

			// Validate students exist and belong to the parent
			var students = new List<Student>();
			foreach (var studentId in request.StudentIds)
			{
				var student = await _studentRepository.FindAsync(studentId);
				if (student == null)
					throw new KeyNotFoundException($"Student with ID {studentId} not found");

				if (student.ParentId != request.ParentId)
					throw new InvalidOperationException(
						$"Student {student.FirstName} {student.LastName} does not belong to the specified parent");

				students.Add(student);
			}

			// Create pickup point using NetTopologySuite Point
			var geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
			var location = geometryFactory.CreatePoint(new NetTopologySuite.Geometries.Coordinate(
				request.Longitude, // X = Longitude
				request.Latitude   // Y = Latitude
			));

			var pickupPoint = new PickupPoint
			{
				Description = request.Description ?? $"Pickup point for parent at {request.AddressText}",
				Location = request.AddressText,
				Geog = location,
				CreatedAt = DateTime.UtcNow
			};

			var createdPickupPoint = await _pickupPointRepository.AddAsync(pickupPoint);

			// Assign students to this pickup point
			foreach (var student in students)
			{
				// Close previous assignment if exists
				if (student.CurrentPickupPointId.HasValue)
				{
					// Create history record for old assignment
					var oldHistory = new StudentPickupPointHistory
					{
						StudentId = student.Id,
						PickupPointId = student.CurrentPickupPointId.Value,
						AssignedAt = student.PickupPointAssignedAt ?? DateTime.UtcNow,
						RemovedAt = DateTime.UtcNow,
						ChangeReason = "Reassigned to new pickup point by admin"
					};
					// Note: You may need to inject IStudentPickupPointHistoryRepository to add this
				}

				// Assign new pickup point
				student.CurrentPickupPointId = createdPickupPoint.Id;
				student.PickupPointAssignedAt = DateTime.UtcNow;
				await _studentRepository.UpdateAsync(student);
			}

			return new AdminCreatePickupPointResponse
			{
				Id = createdPickupPoint.Id,
				ParentId = request.ParentId,
				AddressText = request.AddressText,
				Latitude = request.Latitude,
				Longitude = request.Longitude,
				DistanceKm = request.DistanceKm,
				Description = createdPickupPoint.Description,
				CreatedAt = createdPickupPoint.CreatedAt,
				AssignedStudentIds = request.StudentIds,
				Message = $"Pickup point created successfully and {students.Count} student(s) assigned"
			};
		}
	}
}