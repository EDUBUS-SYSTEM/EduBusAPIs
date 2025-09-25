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
        private readonly IMapper _mapper;

        public RouteService(
            IMongoRepository<Route> routeRepository, 
            IMongoRepository<RouteSchedule> routeScheduleRepository,
            IMongoRepository<Schedule> scheduleRepository,
            IPickupPointRepository pickupPointRepository, 
            IStudentRepository studentRepository, 
            IVehicleRepository vehicleRepository, 
            IMapper mapper)
        {
            _routeRepository = routeRepository;
            _routeScheduleRepository = routeScheduleRepository;
            _scheduleRepository = scheduleRepository;
            _pickupPointRepository = pickupPointRepository;
            _studentRepository = studentRepository;
            _vehicleRepository = vehicleRepository;
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
                ValidateRouteScheduleDates(request.RouteSchedule.EffectiveFrom, request.RouteSchedule.EffectiveTo);
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
                    EffectiveFrom = request.RouteSchedule.EffectiveFrom,
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
                ValidateRouteScheduleDates(request.RouteSchedule.EffectiveFrom, request.RouteSchedule.EffectiveTo);
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

            // Use the repository's DeleteAsync method which sets both IsDeleted = true and IsActive = false
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

            route.IsActive = false;
            route.UpdatedAt = DateTime.UtcNow;

            var updatedRoute = await _routeRepository.UpdateAsync(route);
            return updatedRoute != null;
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
            // Validate EffectiveTo is not before EffectiveFrom
            if (effectiveTo.HasValue && effectiveTo.Value.Date < effectiveFrom.Date)
            {
                throw new InvalidOperationException("EffectiveTo date cannot be before EffectiveFrom date");
            }
        }
    }
}