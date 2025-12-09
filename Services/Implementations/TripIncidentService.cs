using System;
using System.Collections.Generic;
using AutoMapper;
using Constants;
using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using Services.Contracts;
using Services.Models.Common;
using Services.Models.TripIncident;

namespace Services.Implementations
{
    public class TripIncidentService : ITripIncidentService
    {
        private readonly ITripIncidentRepository _incidentRepository;
        private readonly ITripRepository _tripRepository;
        private readonly ISupervisorVehicleRepository _supervisorVehicleRepository;
        private readonly IMongoRepository<Route> _routeRepository;
        private readonly IMapper _mapper;

        public TripIncidentService(
            ITripIncidentRepository incidentRepository,
            ITripRepository tripRepository,
            ISupervisorVehicleRepository supervisorVehicleRepository,
            IMongoRepository<Route> routeRepository,
            IMapper mapper)
        {
            _incidentRepository = incidentRepository;
            _tripRepository = tripRepository;
            _supervisorVehicleRepository = supervisorVehicleRepository;
            _routeRepository = routeRepository;
            _mapper = mapper;
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
            return _mapper.Map<TripIncidentResponseDto>(entity);
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
    }
}

