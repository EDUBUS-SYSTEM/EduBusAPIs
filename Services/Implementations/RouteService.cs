using AutoMapper;
using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using Services.Contracts;
using Services.Models.Route;
using Utils;

namespace Services.Implementations
{
    public class RouteService : IRouteService
    {
        private readonly IMongoRepository<Route> _routeRepository;
        private readonly IMongoRepository<RouteSchedule> _routeScheduleRepository;
        private readonly IMongoRepository<Schedule> _scheduleRepository;
        private readonly IPickupPointRepository _pickupPointRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IVehicleRepository _vehicleRepository;
        private readonly IRouteScheduleService _routeScheduleService;
        private readonly ITripService _tripService;
        private readonly IMapper _mapper;

        public RouteService(
            IMongoRepository<Route> routeRepository, 
            IMongoRepository<RouteSchedule> routeScheduleRepository,
            IMongoRepository<Schedule> scheduleRepository,
            IPickupPointRepository pickupPointRepository, 
            IStudentRepository studentRepository, 
            IVehicleRepository vehicleRepository, 
            IRouteScheduleService routeScheduleService,
            ITripService tripService,
            IMapper mapper)
        {
            _routeRepository = routeRepository;
            _routeScheduleRepository = routeScheduleRepository;
            _scheduleRepository = scheduleRepository;
            _pickupPointRepository = pickupPointRepository;
            _studentRepository = studentRepository;
            _vehicleRepository = vehicleRepository;
            _routeScheduleService = routeScheduleService;
            _tripService = tripService;
            _mapper = mapper;
        }

        public async Task<RouteDto> CreateRouteAsync(CreateRouteRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Validate that vehicle exists
            var isVehicleActive = await _vehicleRepository.IsVehicleActiveAsync(request.VehicleId);
            if (!isVehicleActive)
                throw new InvalidOperationException($"Vehicle with ID {request.VehicleId} does not exist or is not active");
            await ValidateVehicleNotInOtherRoutesAsync(request.VehicleId);
            // Validate that all pickup points exist and are assigned to active students
            var pickupPointIds = request.PickupPoints.Select(pp => pp.PickupPointId).ToList();
            var validPickupPoints = await ValidatePickupPointsAsync(pickupPointIds);

            // Validate pickup points are not used in other active routes
            await ValidatePickupPointsNotInOtherRoutesAsync(pickupPointIds);

            // Validate sequence order uniqueness within the route
            ValidateSequenceOrderUniqueness(request.PickupPoints);

            // Validate ScheduleId if RouteSchedule is provided
            if (request.RouteSchedule != null)
            {
                var schedule = await _scheduleRepository.FindAsync(request.RouteSchedule.ScheduleId);
                if (schedule == null || schedule.IsDeleted || !schedule.IsActive)
                    throw new InvalidOperationException($"Schedule with ID {request.RouteSchedule.ScheduleId} does not exist or is not active");

                // Validate EffectiveFrom and EffectiveTo
                ValidateRouteScheduleDates(request.RouteSchedule.EffectiveFrom ?? DateTime.UtcNow.Date, request.RouteSchedule.EffectiveTo);
            }

            // Create new route with location info from existing pickup points
            var route = new Route
            {
                RouteName = request.RouteName,
                VehicleId = request.VehicleId,
                IsActive = true,
                PickupPoints = request.PickupPoints.Select(pp =>
                {
                    var pickupPoint = validPickupPoints[pp.PickupPointId];
                    return new PickupPointInfo
                    {
                        PickupPointId = pp.PickupPointId,
                        SequenceOrder = pp.SequenceOrder,
                        Location = new LocationInfo
                        {
                            Latitude = pickupPoint.Geog.Y,
                            Longitude = pickupPoint.Geog.X,
                            Address = pickupPoint.Location
                        }
                    };
                }).ToList()
            };

            var createdRoute = await _routeRepository.AddAsync(route);

            // Create RouteSchedule if provided
            if (request.RouteSchedule != null)
            {
                var routeSchedule = new RouteSchedule
                {
                    RouteId = createdRoute.Id,
                    ScheduleId = request.RouteSchedule.ScheduleId,
                    EffectiveFrom = request.RouteSchedule.EffectiveFrom ?? DateTime.UtcNow.Date,
                    EffectiveTo = request.RouteSchedule.EffectiveTo,
                    Priority = request.RouteSchedule.Priority,
                    IsActive = true
                };

                await _routeScheduleRepository.AddAsync(routeSchedule);
            }

            return _mapper.Map<RouteDto>(createdRoute);
        }

        public async Task<CreateBulkRouteResponse> CreateBulkRoutesAsync(CreateBulkRouteRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Routes == null || !request.Routes.Any())
                throw new ArgumentException("Routes list cannot be null or empty", nameof(request));

            var response = new CreateBulkRouteResponse
            {
                TotalRoutes = request.Routes.Count,
                Success = true
            };

            var createdRoutes = new List<RouteDto>();
            var errors = new List<BulkRouteError>();

            // Validate for duplicates within the request
            var validationErrors = ValidateBulkRouteDuplicates(request.Routes);
            if (validationErrors.Any())
            {
                errors.AddRange(validationErrors);
                response.CreatedRoutes = createdRoutes;
                response.Errors = errors;
                response.SuccessfulRoutes = createdRoutes.Count;
                response.FailedRoutes = errors.Count;
                response.Success = false;
                return response;
            }

            // Validate shared schedule if provided
            if (request.RouteSchedule != null)
            {
                var schedule = await _scheduleRepository.FindAsync(request.RouteSchedule.ScheduleId);
                if (schedule == null || schedule.IsDeleted || !schedule.IsActive)
                    throw new InvalidOperationException($"Schedule with ID {request.RouteSchedule.ScheduleId} does not exist or is not active");

                // Validate dates once
                ValidateRouteScheduleDates(request.RouteSchedule.EffectiveFrom ?? DateTime.UtcNow.Date, request.RouteSchedule.EffectiveTo);
            }

            // Process each route individually
            for (int i = 0; i < request.Routes.Count; i++)
            {
                try
                {
                    var routeRequest = request.Routes[i];
                    
                    // Override RouteSchedule with shared schedule if provided
                    if (request.RouteSchedule != null)
                    {
                        routeRequest.RouteSchedule = request.RouteSchedule;
                    }
                    
                    var createdRoute = await CreateRouteAsync(routeRequest);
                    createdRoutes.Add(createdRoute);
                }
                catch (Exception ex)
                {
                    errors.Add(new BulkRouteError
                    {
                        Index = i,
                        RouteName = request.Routes[i].RouteName,
                        ErrorMessage = ex.Message,
                        ErrorCode = ex.GetType().Name
                    });
                }
            }

            response.CreatedRoutes = createdRoutes;
            response.Errors = errors;
            response.SuccessfulRoutes = createdRoutes.Count;
            response.FailedRoutes = errors.Count;
            response.Success = errors.Count == 0;

            return response;
        }

        private List<BulkRouteError> ValidateBulkRouteDuplicates(List<CreateRouteRequest> routes)
        {
            var errors = new List<BulkRouteError>();

            // Check for duplicate route names within the request
            var routeNameGroups = routes
                .Select((route, index) => new { route.RouteName, Index = index })
                .GroupBy(x => x.RouteName.ToLower())
                .Where(g => g.Count() > 1);

            foreach (var group in routeNameGroups)
            {
                foreach (var item in group)
                {
                    errors.Add(new BulkRouteError
                    {
                        Index = item.Index,
                        RouteName = item.RouteName,
                        ErrorMessage = $"Duplicate route name '{item.RouteName}' found in the request",
                        ErrorCode = "DuplicateRouteName"
                    });
                }
            }

            // Check for duplicate vehicle assignments within the request
            var vehicleGroups = routes
                .Select((route, index) => new { route.VehicleId, Index = index })
                .GroupBy(x => x.VehicleId)
                .Where(g => g.Count() > 1);

            foreach (var group in vehicleGroups)
            {
                foreach (var item in group)
                {
                    errors.Add(new BulkRouteError
                    {
                        Index = item.Index,
                        RouteName = routes[item.Index].RouteName,
                        ErrorMessage = $"Vehicle ID '{item.VehicleId}' is assigned to multiple routes in the request",
                        ErrorCode = "DuplicateVehicleAssignment"
                    });
                }
            }

            // Check for duplicate pickup point assignments within the request
            var allPickupPointIds = routes
                .SelectMany((route, routeIndex) => 
                    route.PickupPoints.Select(pp => new { pp.PickupPointId, RouteIndex = routeIndex }))
                .ToList();

            var pickupPointGroups = allPickupPointIds
                .GroupBy(x => x.PickupPointId)
                .Where(g => g.Count() > 1);

            foreach (var group in pickupPointGroups)
            {
                foreach (var item in group)
                {
                    errors.Add(new BulkRouteError
                    {
                        Index = item.RouteIndex,
                        RouteName = routes[item.RouteIndex].RouteName,
                        ErrorMessage = $"Pickup point ID '{item.PickupPointId}' is assigned to multiple routes in the request",
                        ErrorCode = "DuplicatePickupPointAssignment"
                    });
                }
            }

            return errors;
        }

        public async Task<RouteDto?> GetRouteByIdAsync(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Invalid route ID", nameof(id));

            var route = await _routeRepository.FindAsync(id);
            if (route == null || route.IsDeleted)
                return null;

			var routeDto = _mapper.Map<RouteDto>(route);

			// Fetch vehicle capacity
			var vehicle = await _vehicleRepository.FindAsync(route.VehicleId);
			if (vehicle != null)
			{
				routeDto.VehicleCapacity = vehicle.Capacity;
                routeDto.VehicleNumberPlate = SecurityHelper.DecryptFromBytes(vehicle.HashedLicensePlate);
			}

			return routeDto;
		}

		public async Task<IEnumerable<RouteDto>> GetAllRoutesAsync()
		{
			var routes = await _routeRepository.FindByConditionAsync(r => !r.IsDeleted);
			var routeDtos = routes.Select(r => _mapper.Map<RouteDto>(r)).ToList();

			foreach (var routeDto in routeDtos)
			{
				routeDto.PickupPoints = routeDto.PickupPoints
					.OrderBy(pp => pp.SequenceOrder)
					.ToList();
			}

			var allPickupPointIds = routeDtos
				.SelectMany(r => r.PickupPoints)
				.Select(pp => pp.PickupPointId)
				.Distinct()
				.ToList();

			// Get student counts AND student details for each pickup point
			var studentsByPickupPoint = new Dictionary<Guid, List<StudentInfoDto>>();
			if (allPickupPointIds.Any())
			{
				var students = await _studentRepository.FindByConditionAsync(s =>
					allPickupPointIds.Contains(s.CurrentPickupPointId ?? Guid.Empty) &&
					s.Status == StudentStatus.Active &&
					!s.IsDeleted);

				// Group students by pickup point
				studentsByPickupPoint = students
					.Where(s => s.CurrentPickupPointId.HasValue)
					.GroupBy(s => s.CurrentPickupPointId!.Value)
					.ToDictionary(
						g => g.Key,
						g => g.Select(s => new StudentInfoDto
						{
							Id = s.Id,
							FirstName = s.FirstName,
							LastName = s.LastName,
							Status = s.Status,
							PickupPointAssignedAt = s.PickupPointAssignedAt
						}).ToList()
					);
			}

			// Populate student information for each pickup point
			foreach (var routeDto in routeDtos)
			{
				foreach (var pickupPoint in routeDto.PickupPoints)
				{
					if (studentsByPickupPoint.TryGetValue(pickupPoint.PickupPointId, out var students))
					{
						pickupPoint.Students = students;
						pickupPoint.StudentCount = students.Count;
					}
					else
					{
						pickupPoint.Students = new List<StudentInfoDto>();
						pickupPoint.StudentCount = 0;
					}
				}
			}

			var vehicleIds = routeDtos.Select(r => r.VehicleId).Distinct().ToList();
			var vehicles = await _vehicleRepository.FindByConditionAsync(v => vehicleIds.Contains(v.Id));

			foreach (var routeDto in routeDtos)
			{
				var vehicle = vehicles.FirstOrDefault(v => v.Id == routeDto.VehicleId);
				if (vehicle != null)
				{
					routeDto.VehicleCapacity = vehicle.Capacity;
					routeDto.VehicleNumberPlate = SecurityHelper.DecryptFromBytes(vehicle.HashedLicensePlate);
				}
			}

			return routeDtos;
		}

		public async Task<IEnumerable<RouteDto>> GetActiveRoutesAsync()
		{
			var routes = await _routeRepository.FindByConditionAsync(r => !r.IsDeleted && r.IsActive);
			var routeDtos = routes.Select(r => _mapper.Map<RouteDto>(r)).ToList();

			foreach (var routeDto in routeDtos)
			{
				routeDto.PickupPoints = routeDto.PickupPoints
					.OrderBy(pp => pp.SequenceOrder)
					.ToList();
			}

			var allPickupPointIds = routeDtos
				.SelectMany(r => r.PickupPoints)
				.Select(pp => pp.PickupPointId)
				.Distinct()
				.ToList();

			// Get student counts AND student details for each pickup point
			var studentsByPickupPoint = new Dictionary<Guid, List<StudentInfoDto>>();
			if (allPickupPointIds.Any())
			{
				var students = await _studentRepository.FindByConditionAsync(s =>
					allPickupPointIds.Contains(s.CurrentPickupPointId ?? Guid.Empty) &&
					s.Status == StudentStatus.Active &&
					!s.IsDeleted);

				// Group students by pickup point
				studentsByPickupPoint = students
					.Where(s => s.CurrentPickupPointId.HasValue)
					.GroupBy(s => s.CurrentPickupPointId!.Value)
					.ToDictionary(
						g => g.Key,
						g => g.Select(s => new StudentInfoDto
						{
							Id = s.Id,
							FirstName = s.FirstName,
							LastName = s.LastName,
							Status = s.Status,
							PickupPointAssignedAt = s.PickupPointAssignedAt
						}).ToList()
					);
			}

			// Populate student information for each pickup point
			foreach (var routeDto in routeDtos)
			{
				foreach (var pickupPoint in routeDto.PickupPoints)
				{
					if (studentsByPickupPoint.TryGetValue(pickupPoint.PickupPointId, out var students))
					{
						pickupPoint.Students = students;
						pickupPoint.StudentCount = students.Count;
					}
					else
					{
						pickupPoint.Students = new List<StudentInfoDto>();
						pickupPoint.StudentCount = 0;
					}
				}
			}

			var vehicleIds = routeDtos.Select(r => r.VehicleId).Distinct().ToList();
			var vehicles = await _vehicleRepository.FindByConditionAsync(v => vehicleIds.Contains(v.Id));

			foreach (var routeDto in routeDtos)
			{
				var vehicle = vehicles.FirstOrDefault(v => v.Id == routeDto.VehicleId);
				if (vehicle != null)
				{
					routeDto.VehicleCapacity = vehicle.Capacity;
					routeDto.VehicleNumberPlate = SecurityHelper.DecryptFromBytes(vehicle.HashedLicensePlate);
				}
			}

			return routeDtos;
		}

		public async Task<RouteDto?> UpdateRouteAsync(Guid id, UpdateRouteRequest request)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Invalid route ID", nameof(id));

            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var route = await _routeRepository.FindAsync(id);
            if (route == null || route.IsDeleted)
                return null;

            // Check if route name is being changed and if it's unique
            if (!string.IsNullOrEmpty(request.RouteName) &&
                request.RouteName.ToLower() != route.RouteName.ToLower())
            {
                var existingRoutes = await _routeRepository.FindByConditionAsync(r =>
                    r.RouteName.ToLower() == request.RouteName.ToLower() &&
                    !r.IsDeleted && r.Id != id);

                if (existingRoutes.Any())
                    throw new InvalidOperationException("Route name already exists");
            }

            // Update route properties
            if (!string.IsNullOrEmpty(request.RouteName))
                route.RouteName = request.RouteName;

            if (request.VehicleId.HasValue)
            {
                // Validate that vehicle exists and is active
                var isVehicleActive = await _vehicleRepository.IsVehicleActiveAsync(request.VehicleId.Value);
                if (!isVehicleActive)
                    throw new InvalidOperationException($"Vehicle with ID {request.VehicleId.Value} does not exist or is not active");
                await ValidateVehicleNotInOtherRoutesAsync(request.VehicleId.Value, id);
                route.VehicleId = request.VehicleId.Value;
            }

            if (request.PickupPoints != null)
            {

                // Validate that all pickup points exist and are assigned to active students
                var pickupPointIds = request.PickupPoints.Select(pp => pp.PickupPointId).ToList();
                var validPickupPoints = await ValidatePickupPointsAsync(pickupPointIds);

                // Validate pickup points are not used in other active routes (exclude current route)
                await ValidatePickupPointsNotInOtherRoutesAsync(pickupPointIds, id);

                // Validate sequence order uniqueness within the route
                ValidateSequenceOrderUniqueness(request.PickupPoints);

                route.PickupPoints = request.PickupPoints.Select(pp =>
                {
                    var pickupPoint = validPickupPoints[pp.PickupPointId];
                    return new PickupPointInfo
                    {
                        PickupPointId = pp.PickupPointId,
                        SequenceOrder = pp.SequenceOrder,
                        Location = new LocationInfo
                        {
                            Latitude = pickupPoint.Geog.Y,
                            Longitude = pickupPoint.Geog.X,
                            Address = pickupPoint.Location
                        }
                    };
                }).ToList();
            }

            route.UpdatedAt = DateTime.UtcNow;

            var updatedRoute = await _routeRepository.UpdateAsync(route);
            return updatedRoute != null ? _mapper.Map<RouteDto>(updatedRoute) : null;
        }

        public async Task<bool> SoftDeleteRouteAsync(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Invalid route ID", nameof(id));

            var route = await _routeRepository.FindAsync(id);
            if (route == null || route.IsDeleted)
                return false;

            await _routeScheduleService.DeactivateRouteSchedulesByRouteAsync(id);

            await _tripService.CascadeDeactivateTripsByRouteAsync(id);

            var deletedRoute = await _routeRepository.DeleteAsync(id);
            return deletedRoute != null;
        }

        public async Task<bool> ActivateRouteAsync(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Invalid route ID", nameof(id));

            var route = await _routeRepository.FindAsync(id);
            if (route == null || route.IsDeleted)
                return false;

            route.IsActive = true;
            route.UpdatedAt = DateTime.UtcNow;

            var updatedRoute = await _routeRepository.UpdateAsync(route);
            return updatedRoute != null;
        }

        public async Task<bool> DeactivateRouteAsync(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Invalid route ID", nameof(id));

            var route = await _routeRepository.FindAsync(id);
            if (route == null || route.IsDeleted)
                return false;

            await _routeScheduleService.DeactivateRouteSchedulesByRouteAsync(id);

            await _tripService.CascadeDeactivateTripsByRouteAsync(id);

            route.IsActive = false;
            route.UpdatedAt = DateTime.UtcNow;

            var updatedRoute = await _routeRepository.UpdateAsync(route);
            return updatedRoute != null;
        }

		public async Task<UpdateBulkRouteResponse> UpdateBulkRoutesAsync(UpdateBulkRouteRequest request)
		{
			if (request == null)
				throw new ArgumentNullException(nameof(request));

			if (request.Routes == null || !request.Routes.Any())
				throw new ArgumentException("Routes list cannot be null or empty", nameof(request));

			var response = new UpdateBulkRouteResponse
			{
				TotalRoutes = request.Routes.Count,
				Success = false
			};

			try
			{
				// Validate bulk update context
				var validationErrors = await ValidateBulkUpdateContextAsync(request.Routes);
				if (validationErrors.Any())
				{
					response.ErrorMessage = $"Validation failed: {string.Join(", ", validationErrors)}";
					return response;
				}

				// Get all routes that need to be updated
				var routeIds = request.Routes.Select(r => r.RouteId).ToList();
				var existingRoutes = await _routeRepository.FindByConditionAsync(r => routeIds.Contains(r.Id));
				var existingRoutesDict = existingRoutes.ToDictionary(r => r.Id);

				// Prepare all updates
				var updatedRoutes = new List<Route>();
				foreach (var routeItem in request.Routes)
				{
					if (!existingRoutesDict.TryGetValue(routeItem.RouteId, out var existingRoute))
					{
						throw new InvalidOperationException($"Route with ID {routeItem.RouteId} not found");
					}

					// Update route properties
					if (!string.IsNullOrEmpty(routeItem.RouteName))
						existingRoute.RouteName = routeItem.RouteName;

					if (routeItem.VehicleId.HasValue)
					{
						var isVehicleActive = await _vehicleRepository.IsVehicleActiveAsync(routeItem.VehicleId.Value);
						if (!isVehicleActive)
							throw new InvalidOperationException($"Vehicle with ID {routeItem.VehicleId.Value} does not exist or is not active");

						await ValidateVehicleNotInOtherRoutesAsync(routeItem.VehicleId.Value, routeItem.RouteId);
						existingRoute.VehicleId = routeItem.VehicleId.Value;
					}

					if (routeItem.PickupPoints != null)
					{
						// Validate that all pickup points exist and are assigned to active students
						var pickupPointIds = routeItem.PickupPoints.Select(pp => pp.PickupPointId).ToList();
						var validPickupPoints = await ValidatePickupPointsAsync(pickupPointIds);

						// Validate sequence order uniqueness within the route
						ValidateSequenceOrderUniqueness(routeItem.PickupPoints);

						existingRoute.PickupPoints = routeItem.PickupPoints.Select(pp =>
						{
							var pickupPoint = validPickupPoints[pp.PickupPointId];
							return new PickupPointInfo
							{
								PickupPointId = pp.PickupPointId,
								SequenceOrder = pp.SequenceOrder,
								Location = new LocationInfo
								{
									Latitude = pickupPoint.Geog.Y,
									Longitude = pickupPoint.Geog.X,
									Address = pickupPoint.Location
								}
							};
						}).ToList();
					}

					existingRoute.UpdatedAt = DateTime.UtcNow;
					updatedRoutes.Add(existingRoute);
				}

				var updatedRouteResults = await _routeRepository.BulkUpdateAsync(updatedRoutes);

				// Map updated routes to DTOs
				var updatedRouteDtos = updatedRouteResults.Select(r => _mapper.Map<RouteDto>(r)).ToList();

				// Populate vehicle information
				var vehicleIds = updatedRouteDtos.Select(r => r.VehicleId).Distinct().ToList();
				var vehicles = await _vehicleRepository.FindByConditionAsync(v => vehicleIds.Contains(v.Id));

				foreach (var routeDto in updatedRouteDtos)
				{
					var vehicle = vehicles.FirstOrDefault(v => v.Id == routeDto.VehicleId);
					if (vehicle != null)
					{
						routeDto.VehicleCapacity = vehicle.Capacity;
						routeDto.VehicleNumberPlate = SecurityHelper.DecryptFromBytes(vehicle.HashedLicensePlate);
					}
				}

				response.Success = true;
				response.UpdatedRoutes = updatedRouteDtos;

				return response;
			}
			catch (Exception ex)
			{
				response.ErrorMessage = ex.Message;
				return response;
			}
		}

		public async Task<RouteDto?> UpdateRouteBasicAsync(Guid id, UpdateRouteBasicRequest request)
		{
			if (id == Guid.Empty)
				throw new ArgumentException("Invalid route ID", nameof(id));

			if (request == null)
				throw new ArgumentNullException(nameof(request));

			var route = await _routeRepository.FindAsync(id);
			if (route == null || route.IsDeleted)
				return null;

			// Check if route name is being changed and if it's unique
			if (!string.IsNullOrEmpty(request.RouteName) &&
				request.RouteName.ToLower() != route.RouteName.ToLower())
			{
				var existingRoutes = await _routeRepository.FindByConditionAsync(r =>
					r.RouteName.ToLower() == request.RouteName.ToLower() &&
					!r.IsDeleted && r.Id != id);

				if (existingRoutes.Any())
					throw new InvalidOperationException("Route name already exists");
			}

			// Update route properties (only name and vehicle)
			if (!string.IsNullOrEmpty(request.RouteName))
				route.RouteName = request.RouteName;

			if (request.VehicleId.HasValue)
			{
				// Validate that vehicle exists and is active
				var isVehicleActive = await _vehicleRepository.IsVehicleActiveAsync(request.VehicleId.Value);
				if (!isVehicleActive)
					throw new InvalidOperationException($"Vehicle with ID {request.VehicleId.Value} does not exist or is not active");

				await ValidateVehicleNotInOtherRoutesAsync(request.VehicleId.Value, id);
				route.VehicleId = request.VehicleId.Value;
			}

			// Do NOT modify pickup points - leave them unchanged
			route.UpdatedAt = DateTime.UtcNow;

			var updatedRoute = await _routeRepository.UpdateAsync(route);
			if (updatedRoute == null) return null;

			var routeDto = _mapper.Map<RouteDto>(updatedRoute);

			// Get vehicle information
			var vehicle = await _vehicleRepository.FindAsync(updatedRoute.VehicleId);
			if (vehicle != null)
			{
				routeDto.VehicleCapacity = vehicle.Capacity;
				routeDto.VehicleNumberPlate = SecurityHelper.DecryptFromBytes(vehicle.HashedLicensePlate);
			}

			return routeDto;
		}

		public async Task<ReplaceAllRoutesResponse> ReplaceAllRoutesAsync(ReplaceAllRoutesRequest request)
		{
			if (request == null)
				throw new ArgumentNullException(nameof(request));

			if (request.Routes == null || !request.Routes.Any())
				throw new ArgumentException("Routes list cannot be null or empty", nameof(request));

			var response = new ReplaceAllRoutesResponse
			{
				TotalNewRoutes = request.Routes.Count,
				Success = false
			};

			try
			{
				var validationErrors = ValidateBulkRouteDuplicates(request.Routes);
				if (validationErrors.Any())
				{
					response.ErrorMessage = $"Validation failed: {string.Join(", ", validationErrors.Select(e => e.ErrorMessage))}";
					return response;
				}

				// Validate each route individually
				var routesToCreate = new List<Route>();
				for (int i = 0; i < request.Routes.Count; i++)
				{
					var routeRequest = request.Routes[i];

					// Prepare route object
					var newRoute = new Route
					{
						Id = Guid.NewGuid(),
						RouteName = routeRequest.RouteName,
						VehicleId = routeRequest.VehicleId,
						IsActive = true,
						IsDeleted = false
					};

					// Add pickup points if provided
					if (routeRequest.PickupPoints?.Any() == true)
					{
						var pickupPointIds = routeRequest.PickupPoints.Select(pp => pp.PickupPointId).ToList();
						var validPickupPoints = await ValidatePickupPointsAsync(pickupPointIds);

						ValidateSequenceOrderUniqueness(routeRequest.PickupPoints);

						newRoute.PickupPoints = routeRequest.PickupPoints.Select(pp =>
						{
							var pickupPoint = validPickupPoints[pp.PickupPointId];
							return new PickupPointInfo
							{
								PickupPointId = pp.PickupPointId,
								SequenceOrder = pp.SequenceOrder,
								Location = new LocationInfo
								{
									Latitude = pickupPoint.Geog.Y,
									Longitude = pickupPoint.Geog.X,
									Address = pickupPoint.Location
								}
							};
						}).ToList();
					}

					routesToCreate.Add(newRoute);
				}

				var existingRoutes = await _routeRepository.FindByConditionAsync(r => !r.IsDeleted);
				var existingRouteIds = existingRoutes.Select(r => r.Id).ToList();

				var deletedCount = 0;
				if (existingRouteIds.Any())
				{
					var deleteResult = await _routeRepository.BulkDeleteAsync(existingRouteIds);
					deletedCount = (int)deleteResult.ModifiedCount;
				}

				response.DeletedRoutes = deletedCount;

				var createdRoutes = await _routeRepository.BulkCreateAsync(routesToCreate);
				var createdRoutesList = createdRoutes.ToList();

				var routeDtos = new List<RouteDto>();

				// Get all vehicle information in one query
				var vehicleIds = createdRoutesList.Select(r => r.VehicleId).Distinct().ToList();
				var vehicles = await _vehicleRepository.FindByConditionAsync(v => vehicleIds.Contains(v.Id));
				var vehicleDict = vehicles.ToDictionary(v => v.Id);

				// Get all pickup point IDs for student count queries
				var allPickupPointIds = createdRoutesList
					.SelectMany(r => r.PickupPoints)
					.Select(pp => pp.PickupPointId)
					.Distinct()
					.ToList();

				// Get student counts in one query
				var studentCounts = new Dictionary<Guid, int>();
				if (allPickupPointIds.Any())
				{
					var students = await _studentRepository.FindByConditionAsync(s =>
						allPickupPointIds.Contains(s.CurrentPickupPointId ?? Guid.Empty) &&
						s.Status == StudentStatus.Active &&
						!s.IsDeleted);

					studentCounts = students
						.Where(s => s.CurrentPickupPointId.HasValue)
						.GroupBy(s => s.CurrentPickupPointId!.Value)
						.ToDictionary(g => g.Key, g => g.Count());
				}

				// Build DTOs
				foreach (var route in createdRoutesList)
				{
					var routeDto = _mapper.Map<RouteDto>(route);

					// Add vehicle information
					if (vehicleDict.TryGetValue(route.VehicleId, out var vehicle))
					{
						routeDto.VehicleCapacity = vehicle.Capacity;
						routeDto.VehicleNumberPlate = SecurityHelper.DecryptFromBytes(vehicle.HashedLicensePlate);
					}

					// Add student counts and sort pickup points
					foreach (var pickupPoint in routeDto.PickupPoints)
					{
						pickupPoint.StudentCount = studentCounts.GetValueOrDefault(pickupPoint.PickupPointId, 0);
					}
					routeDto.PickupPoints = routeDto.PickupPoints.OrderBy(pp => pp.SequenceOrder).ToList();

					routeDtos.Add(routeDto);
				}

				response.CreatedRoutes = routeDtos;
				response.SuccessfulRoutes = routeDtos.Count;
				response.Success = true;

				return response;
			}
			catch (Exception ex)
			{
				response.ErrorMessage = $"Failed to replace routes: {ex.Message}";
				return response;
			}
		}

		private async Task<List<string>> ValidateBulkUpdateContextAsync(List<UpdateBulkRouteItem> routes)
		{
			var errors = new List<string>();

			// Collect all pickup points from the bulk update
			var allPickupPointsInBulkUpdate = routes
				.Where(r => r.PickupPoints != null)
				.SelectMany(r => r.PickupPoints!.Select(pp => new { RouteId = r.RouteId, PickupPointId = pp.PickupPointId }))
				.ToList();

			// Group pickup points by pickup point ID to find duplicates within the bulk update
			var pickupPointGroups = allPickupPointsInBulkUpdate
				.GroupBy(x => x.PickupPointId)
				.Where(g => g.Count() > 1)
				.ToList();

			foreach (var group in pickupPointGroups)
			{
				var routeIds = group.Select(x => x.RouteId).Distinct();
				errors.Add($"Pickup point {group.Key} is assigned to multiple routes in this update: {string.Join(", ", routeIds)}");
			}

			// Check for conflicts with routes NOT in the bulk update
			var bulkUpdateRouteIds = routes.Select(r => r.RouteId).ToHashSet();
			var allPickupPointIds = allPickupPointsInBulkUpdate.Select(x => x.PickupPointId).Distinct().ToList();

			if (allPickupPointIds.Any())
			{
				// Find routes outside the bulk update that use these pickup points
				var conflictingRoutes = await _routeRepository.FindByConditionAsync(r =>
					!r.IsDeleted &&
					r.IsActive &&
					!bulkUpdateRouteIds.Contains(r.Id) && // Exclude routes being updated
					r.PickupPoints.Any(pp => allPickupPointIds.Contains(pp.PickupPointId)));

				if (conflictingRoutes.Any())
				{
					var conflicts = conflictingRoutes
						.Select(r => new
						{
							r.Id,
							r.RouteName,
							ConflictingPickupPoints = r.PickupPoints
								.Where(pp => allPickupPointIds.Contains(pp.PickupPointId))
								.Select(pp => pp.PickupPointId)
								.ToList()
						})
						.ToList();

					var errorMessages = conflicts.Select(cr =>
						$"Route '{cr.RouteName}' already uses pickup point {string.Join(", ", cr.ConflictingPickupPoints)}");

					errors.AddRange(errorMessages);
				}
			}

			return errors;
		}

		// check pickupoint is exist
		private async Task<Dictionary<Guid, PickupPoint>> ValidatePickupPointsAsync(List<Guid> pickupPointIds)
        {
            // Get pickup points that exist and are not deleted
            var existingPickupPoints = await _pickupPointRepository.FindByConditionAsync(pp => 
                pickupPointIds.Contains(pp.Id) && !pp.IsDeleted);

            // Get active students who have these pickup points as their current pickup point
            var activeStudents = await _studentRepository.FindByConditionAsync(s => 
                pickupPointIds.Contains(s.CurrentPickupPointId ?? Guid.Empty) && 
                s.Status == StudentStatus.Active && 
                !s.IsDeleted);

            var activePickupPointIds = activeStudents
                .Where(s => s.CurrentPickupPointId.HasValue)
                .Select(s => s.CurrentPickupPointId!.Value)
                .ToList();

            var missingPickupPointIds = pickupPointIds.Except(activePickupPointIds).ToList();

            if (missingPickupPointIds.Any())
            {
                throw new InvalidOperationException($"The following pickup points are not assigned to active students: {string.Join(", ", missingPickupPointIds)}");
            }

            
            return existingPickupPoints.ToDictionary(pp => pp.Id, pp => pp);
        }

        private async Task ValidatePickupPointsNotInOtherRoutesAsync(List<Guid> pickupPointIds, Guid? excludeRouteId = null)
        {
            if (pickupPointIds == null || !pickupPointIds.Any())
                return;

            // Query chỉ lấy routes có pickup points nằm trong danh sách cần check
            var conflictingRoutes = await _routeRepository.FindByConditionAsync(r =>
                !r.IsDeleted &&
                r.IsActive &&
                (!excludeRouteId.HasValue || r.Id != excludeRouteId.Value) &&
                r.PickupPoints.Any(pp => pickupPointIds.Contains(pp.PickupPointId)));

            if (conflictingRoutes.Any())
            {
                var conflicts = conflictingRoutes
                    .Select(r => new
                    {
                        r.Id,
                        r.RouteName,
                        ConflictingPickupPoints = r.PickupPoints
                            .Where(pp => pickupPointIds.Contains(pp.PickupPointId))
                            .Select(pp => pp.PickupPointId)
                            .ToList()
                    })
                    .ToList();

                var errorMessages = conflicts.Select(cr =>
                    $"Route '{cr.RouteName}' already uses pickup point {string.Join(", ", cr.ConflictingPickupPoints)}");

                throw new InvalidOperationException(
                    "The following pickup points are already used in other active routes:\n" +
                    string.Join("\n", errorMessages));
            }
        }


        // check sequence order uniqueness
        private void ValidateSequenceOrderUniqueness(List<RoutePickupPointRequest> pickupPoints)
        {
            var duplicateSequenceOrders = pickupPoints
                .GroupBy(pp => pp.SequenceOrder)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateSequenceOrders.Any())
            {
                throw new InvalidOperationException($"Duplicate sequence orders found: {string.Join(", ", duplicateSequenceOrders)}");
            }
        }
        // check vehicle is not used in other active routes
        private async Task ValidateVehicleNotInOtherRoutesAsync(Guid vehicleId, Guid? excludeRouteId = null)
        {
            var existingRoutes = await _routeRepository.FindByConditionAsync(r =>
                !r.IsDeleted &&
                r.IsActive &&
                (!excludeRouteId.HasValue || r.Id != excludeRouteId.Value) &&
                r.VehicleId == vehicleId);

            if (existingRoutes.Any())
            {
                // Get vehicle info to show license plate instead of ID
                var vehicle = await _vehicleRepository.FindAsync(vehicleId);
                var licensePlate = vehicle != null ? SecurityHelper.DecryptFromBytes(vehicle.HashedLicensePlate) : "Unknown";

                var conflictingRoutes = existingRoutes.Select(r =>
                    $"Route '{r.RouteName}'")
                    .ToList();

                throw new InvalidOperationException(
                    $"Vehicle with license plate '{licensePlate}' is already used in other active routes:\n" +
                    string.Join("\n", conflictingRoutes));
            }
        }

        private void ValidateRouteScheduleDates(DateTime effectiveFrom, DateTime? effectiveTo)
        {
            var currentDate = DateTime.UtcNow.Date;
            
            if (effectiveFrom.Date < currentDate)
            {
                throw new InvalidOperationException($"EffectiveFrom date ({effectiveFrom:yyyy-MM-dd}) cannot be in the past. Current date is {currentDate:yyyy-MM-dd}");
            }
            
            if (effectiveTo.HasValue && effectiveTo.Value.Date < effectiveFrom.Date)
            {
                throw new InvalidOperationException("EffectiveTo date cannot be before EffectiveFrom date");
            }
        }
    }
}