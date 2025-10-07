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
	}
}