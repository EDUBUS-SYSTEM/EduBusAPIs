using AutoMapper;
using Constants;
using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Services.Contracts;
using Services.Models.Common;
using Services.Models.Notification;
using Services.Models.TripIncident;

namespace Services.Implementations
{
    public class TripIncidentService : ITripIncidentService
    {
        private readonly ITripIncidentRepository _incidentRepository;
        private readonly ITripRepository _tripRepository;
        private readonly ISupervisorVehicleRepository _supervisorVehicleRepository;
        private readonly IMongoRepository<Route> _routeRepository;
        private readonly INotificationService _notificationService;
        private readonly ITripHubService? _tripHubService;
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IMapper _mapper;
        private readonly ILogger<TripIncidentService> _logger;

        public TripIncidentService(
            ITripIncidentRepository incidentRepository,
            ITripRepository tripRepository,
            ISupervisorVehicleRepository supervisorVehicleRepository,
            IMongoRepository<Route> routeRepository,
            INotificationService notificationService,
            IUserAccountRepository userAccountRepository,
            IServiceScopeFactory serviceScopeFactory,
            IMapper mapper,
            ILogger<TripIncidentService> logger,
            ITripHubService? tripHubService = null)
        {
            _incidentRepository = incidentRepository;
            _tripRepository = tripRepository;
            _supervisorVehicleRepository = supervisorVehicleRepository;
            _routeRepository = routeRepository;
            _notificationService = notificationService;
            _userAccountRepository = userAccountRepository;
            _serviceScopeFactory = serviceScopeFactory;
            _tripHubService = tripHubService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<TripIncidentResponseDto> CreateAsync(Guid tripId, CreateTripIncidentRequestDto request, Guid supervisorId)
        {
            var trip = await EnsureTripAccessAsync(tripId, supervisorId, isAdmin: false);

            if (!string.Equals(trip.Status, TripConstants.TripStatus.InProgress, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Trip is not InProgress. Incident reports can only be created during an active trip.");
            }

            var route = await _routeRepository.FindAsync(trip.RouteId);
            var normalizedTitle = string.IsNullOrWhiteSpace(request.Title)
                ? request.Reason.ToString()
                : request.Title.Trim();

            if (request.Reason == TripIncidentReason.Other && string.IsNullOrWhiteSpace(normalizedTitle))
            {
                throw new ArgumentException("Title is required when reason is Other.");
            }

            var entity = new TripIncidentReport
            {
                TripId = trip.Id,
                SupervisorId = supervisorId,
                Reason = request.Reason,
                Title = normalizedTitle,
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                Status = TripIncidentStatus.Open,
                ServiceDate = trip.ServiceDate,
                TripStatus = trip.Status,
                RouteName = route?.RouteName ?? trip.ScheduleSnapshot?.Name ?? string.Empty,
                VehiclePlate = trip.Vehicle?.MaskedPlate ?? string.Empty,
                SupervisorName = trip.Supervisor?.FullName ?? string.Empty
            };

            await _incidentRepository.AddAsync(entity);
            var response = _mapper.Map<TripIncidentResponseDto>(entity);

            _ = Task.Run(async () =>
            {
                using var scope = _serviceScopeFactory.CreateScope();
                try
                {
                    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                    var userAccountRepository = scope.ServiceProvider.GetRequiredService<IUserAccountRepository>();
                    var tripHubService = scope.ServiceProvider.GetService<ITripHubService>();

                    await SendIncidentNotificationsAsync(entity, trip, notificationService, userAccountRepository, tripHubService);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send incident notifications for incident {IncidentId}", entity.Id);
                }
            });

            return response;
        }

        public async Task<TripIncidentListResponse> GetByTripAsync(Guid tripId, Guid requesterId, bool isAdmin, int page, int perPage)
        {
            (page, perPage) = NormalizePaging(page, perPage);

            await EnsureTripAccessAsync(tripId, requesterId, isAdmin);

            var (items, totalCount) = await _incidentRepository.GetByTripAsync(tripId, page, perPage);

            return new TripIncidentListResponse
            {
                Data = _mapper.Map<List<TripIncidentListItemDto>>(items),
                Pagination = BuildPagination(totalCount, page, perPage)
            };
        }

        public async Task<TripIncidentListResponse> GetAllAsync(Guid? tripId, Guid? supervisorId, TripIncidentStatus? status, int page, int perPage)
        {
            (page, perPage) = NormalizePaging(page, perPage);

            var (items, totalCount) = await _incidentRepository.GetAllAsync(tripId, supervisorId, status, page, perPage);

            return new TripIncidentListResponse
            {
                Data = _mapper.Map<List<TripIncidentListItemDto>>(items),
                Pagination = BuildPagination(totalCount, page, perPage)
            };
        }

        public async Task<TripIncidentResponseDto?> GetByIdAsync(Guid incidentId, Guid requesterId, bool isAdmin)
        {
            var incident = await _incidentRepository.FindAsync(incidentId);
            if (incident == null || incident.IsDeleted)
                return null;

            await EnsureTripAccessAsync(incident.TripId, requesterId, isAdmin);
            return _mapper.Map<TripIncidentResponseDto>(incident);
        }

        public async Task<TripIncidentResponseDto> UpdateStatusAsync(Guid incidentId, UpdateTripIncidentStatusDto request, Guid adminId)
        {
            var incident = await _incidentRepository.FindAsync(incidentId);
            if (incident == null || incident.IsDeleted)
                throw new KeyNotFoundException("Incident not found.");

            incident.Status = request.Status;
            incident.AdminNote = string.IsNullOrWhiteSpace(request.AdminNote) ? null : request.AdminNote.Trim();
            incident.HandledBy = adminId;
            incident.HandledAt = DateTime.UtcNow;

            await _incidentRepository.UpdateAsync(incident);
            return _mapper.Map<TripIncidentResponseDto>(incident);
        }

        private async Task<Trip> EnsureTripAccessAsync(Guid tripId, Guid requesterId, bool isAdmin)
        {
            var trip = await _tripRepository.FindAsync(tripId);
            if (trip == null || trip.IsDeleted)
                throw new KeyNotFoundException("Trip not found.");

            if (isAdmin)
                return trip;

            var assignment = await _supervisorVehicleRepository.GetActiveSupervisorVehicleForVehicleByDateAsync(trip.VehicleId, trip.ServiceDate);
            if (assignment == null || assignment.SupervisorId != requesterId)
                throw new UnauthorizedAccessException("You are not assigned to this trip.");

            return trip;
        }

        private static (int Page, int PerPage) NormalizePaging(int page, int perPage)
        {
            if (page < 1)
                page = 1;

            if (perPage < 1 || perPage > 100)
                perPage = 20;

            return (page, perPage);
        }

        private static PaginationInfo BuildPagination(long totalCount, int page, int perPage)
        {
            var totalPages = (int)Math.Ceiling((double)totalCount / perPage);
            return new PaginationInfo
            {
                CurrentPage = page,
                PerPage = perPage,
                TotalItems = (int)totalCount,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }

        private async Task SendIncidentNotificationsAsync(
            TripIncidentReport incident,
            Trip trip,
            INotificationService notificationService,
            IUserAccountRepository userAccountRepository,
            ITripHubService? tripHubService)
        {
            try
            {
                var adminUsers = await userAccountRepository.GetAdminUsersAsync();
                if (!adminUsers.Any())
                {
                    _logger.LogWarning("No admin users found to send incident notification");
                    return;
                }

                var reasonText = incident.Reason == TripIncidentReason.Other
                    ? incident.Title
                    : incident.Reason.ToString();

                var notificationTitle = "New Trip Incident Report";
                var notificationMessage = $"Supervisor {incident.SupervisorName} reported an incident: {reasonText} on route {incident.RouteName}.";

                foreach (var admin in adminUsers)
                {
                    var notificationDto = new CreateNotificationDto
                    {
                        UserId = admin.Id,
                        Title = notificationTitle,
                        Message = notificationMessage,
                        NotificationType = NotificationType.TripInfo,
                        RecipientType = RecipientType.Admin,
                        Priority = 3,
                        RelatedEntityId = incident.Id,
                        RelatedEntityType = "TripIncident",
                        ActionRequired = true,
                        ActionUrl = $"/admin/driver-requests?tab=incidents&incidentId={incident.Id}",
                        Metadata = new Dictionary<string, object>
                        {
                            { "incidentId", incident.Id.ToString() },
                            { "tripId", incident.TripId.ToString() },
                            { "supervisorId", incident.SupervisorId.ToString() },
                            { "supervisorName", incident.SupervisorName },
                            { "reason", incident.Reason.ToString() },
                            { "status", incident.Status.ToString() },
                            { "routeName", incident.RouteName },
                            { "vehiclePlate", incident.VehiclePlate }
                        }
                    };

                    await notificationService.CreateNotificationAsync(notificationDto);
                }

                if (tripHubService != null)
                {
                    await tripHubService.BroadcastIncidentCreatedAsync(incident);
                }

                _logger.LogInformation("Sent incident notifications to {AdminCount} admins for incident {IncidentId}",
                    adminUsers.Count(), incident.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending incident notifications for incident {IncidentId}", incident.Id);
            }
        }
    }
}

