using Data.Models;
using Data.Repos.Interfaces;
using Services.Contracts;
using Services.Models.Trip;
using Utils;
using Constants;

namespace Services.Implementations
{
    public class SupervisorTripService : ISupervisorTripService
    {
        private readonly ISupervisorVehicleRepository _supervisorVehicleRepo;
        private readonly IMongoRepository<Trip> _tripRepository;
        private readonly IMongoRepository<Route> _routeRepository;
        private readonly IPickupPointRepository _pickupPointRepository;
        private readonly IStudentRepository _studentRepository;

        public SupervisorTripService(
            ISupervisorVehicleRepository supervisorVehicleRepo,
            IMongoRepository<Trip> tripRepository,
            IMongoRepository<Route> routeRepository,
            IPickupPointRepository pickupPointRepository,
            IStudentRepository studentRepository)
        {
            _supervisorVehicleRepo = supervisorVehicleRepo;
            _tripRepository = tripRepository;
            _routeRepository = routeRepository;
            _pickupPointRepository = pickupPointRepository;
            _studentRepository = studentRepository;
        }

        public async Task<IEnumerable<SupervisorTripListItemDto>> GetSupervisorTripsAsync(
            Guid supervisorId,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            string? status = null)
        {
            // Get all active assignments for the supervisor
            var assignments = await _supervisorVehicleRepo.GetActiveAssignmentsBySupervisorAsync(supervisorId);
            if (!assignments.Any())
                return Enumerable.Empty<SupervisorTripListItemDto>();

            var now = DateTime.UtcNow;
            var from = dateFrom ?? now.Date;
            var to = dateTo ?? now.Date.AddDays(7);

            // Get trips for vehicles assigned to this supervisor, filtered by assignment time period
            var allTrips = await _tripRepository.FindByConditionAsync(t =>
                t.ServiceDate >= from &&
                t.ServiceDate <= to);

            // Filter trips: must be for assigned vehicles AND within assignment time period
            var filteredTrips = new List<Trip>();
            foreach (var assignment in assignments)
            {
                var assignmentStart = assignment.StartTimeUtc.Date;
                var assignmentEnd = assignment.EndTimeUtc?.Date ?? DateTime.MaxValue.Date;

                var tripsForThisAssignment = allTrips.Where(t =>
                    t.VehicleId == assignment.VehicleId &&
                    t.ServiceDate >= assignmentStart &&
                    t.ServiceDate <= assignmentEnd);

                filteredTrips.AddRange(tripsForThisAssignment);
            }

            var trips = filteredTrips
                .GroupBy(t => t.Id)
                .Select(g => g.First())
                .Where(t => !t.IsDeleted)  
                .ToList();

            if (!string.IsNullOrWhiteSpace(status))
                trips = trips.Where(t => t.Status == status).ToList();

            var result = new List<SupervisorTripListItemDto>();

            foreach (var trip in trips)
            {
                var route = await _routeRepository.FindAsync(trip.RouteId);
                var routeName = route?.RouteName ?? "Unknown Route";

                var completedStops = trip.Stops?.Count(s => s.ArrivedAt.HasValue) ?? 0;
                var totalStops = trip.Stops?.Count ?? 0;

                result.Add(new SupervisorTripListItemDto
                {
                    Id = trip.Id,
                    ServiceDate = trip.ServiceDate,
                    PlannedStartAt = trip.PlannedStartAt,
                    PlannedEndAt = trip.PlannedEndAt,
                    StartTime = trip.StartTime,
                    EndTime = trip.EndTime,
                    Status = trip.Status,
                    RouteName = routeName,
                    Vehicle = trip.Vehicle != null ? new VehicleSnapshotDto
                    {
                        Id = trip.Vehicle.Id,
                        MaskedPlate = trip.Vehicle.MaskedPlate,
                        Capacity = trip.Vehicle.Capacity,
                        Status = trip.Vehicle.Status
                    } : null,
                    Driver = trip.Driver != null ? new DriverSnapshotDto
                    {
                        Id = trip.Driver.Id,
                        FullName = trip.Driver.FullName,
                        Phone = trip.Driver.Phone,
                        IsPrimary = trip.Driver.IsPrimary,
                        SnapshottedAtUtc = trip.Driver.SnapshottedAtUtc
                    } : null,
                    TotalStops = totalStops,
                    CompletedStops = completedStops
                });
            }

            return result.OrderByDescending(t => t.ServiceDate);
        }

        public async Task<SupervisorTripDetailDto?> GetSupervisorTripDetailAsync(Guid tripId, Guid supervisorId)
        {
            // Get trip
            var trip = await _tripRepository.FindAsync(tripId);
            if (trip == null || trip.IsDeleted)
                return null;

            // Authorization check: supervisor must be assigned to this vehicle
            var assignment = await _supervisorVehicleRepo.GetActiveSupervisorVehicleForVehicleByDateAsync(trip.VehicleId, trip.ServiceDate);
            if (assignment == null || assignment.SupervisorId != supervisorId)
                return null; // Not authorized

            // Get route name
            var route = await _routeRepository.FindAsync(trip.RouteId);
            var routeName = route?.RouteName ?? "Unknown Route";

            // Map stops
            var stops = new List<SupervisorTripStopDto>();
            if (trip.Stops != null && trip.Stops.Any())
            {
                foreach (var stop in trip.Stops.OrderBy(s => s.SequenceOrder))
                {
                    // Get address from stop.Location
                    var address = stop.Location?.Address ?? "Unknown Stop";
                    
                    // Get student image IDs for all students in this stop
                    var studentIds = stop.Attendance?.Select(a => a.StudentId).Distinct().ToList() ?? new List<Guid>();
                    var students = new Dictionary<Guid, Guid?>();
                    foreach (var studentId in studentIds)
                    {
                        try
                        {
                            var student = await _studentRepository.FindAsync(studentId);
                            students[studentId] = student?.StudentImageId;
                        }
                        catch
                        {
                            students[studentId] = null;
                        }
                    }

                    var attendance = stop.Attendance?
                        .Select(a => new SupervisorAttendanceDto
                        {
                            StudentId = a.StudentId,
                            StudentName = a.StudentName,
                            ClassName = string.Empty,
                            StudentImageId = students.GetValueOrDefault(a.StudentId),
                            BoardedAt = a.BoardedAt,
                            AlightedAt = a.AlightedAt,
                            State = a.State,
                            BoardStatus = a.BoardStatus,
                            AlightStatus = a.AlightStatus
                        })
                        .ToList() ?? new List<SupervisorAttendanceDto>();

                    stops.Add(new SupervisorTripStopDto
                    {
                        Id = stop.PickupPointId,
                        Name = address,
                        PlannedArrival = stop.PlannedAt,
                        ActualArrival = stop.ArrivedAt,
                        PlannedDeparture = stop.PlannedAt.AddMinutes(5), // Estimate
                        ActualDeparture = stop.DepartedAt,
                        Sequence = stop.SequenceOrder,
                        Location = new StopLocationDto
                        {
                            Latitude = stop.Location?.Latitude ?? 0,
                            Longitude = stop.Location?.Longitude ?? 0,
                            Address = address
                        },
                        Attendance = attendance
                    });
                }
            }

            return new SupervisorTripDetailDto
            {
                Id = trip.Id,
                ServiceDate = trip.ServiceDate,
                PlannedStartAt = trip.PlannedStartAt,
                PlannedEndAt = trip.PlannedEndAt,
                StartTime = trip.StartTime,
                EndTime = trip.EndTime,
                Status = trip.Status,
                RouteName = routeName,
                Vehicle = trip.Vehicle != null ? new VehicleSnapshotDto
                {
                    Id = trip.Vehicle.Id,
                    MaskedPlate = trip.Vehicle.MaskedPlate,
                    Capacity = trip.Vehicle.Capacity,
                    Status = trip.Vehicle.Status
                } : null,
                Driver = trip.Driver != null ? new DriverSnapshotDto
                {
                    Id = trip.Driver.Id,
                    FullName = trip.Driver.FullName,
                    Phone = trip.Driver.Phone,
                    IsPrimary = trip.Driver.IsPrimary,
                    SnapshottedAtUtc = trip.Driver.SnapshottedAtUtc
                } : null,
                Stops = stops
            };
        }

        public Task<IEnumerable<SupervisorTripListItemDto>> GetTodayTripsAsync(Guid supervisorId)
        {
            var today = DateTime.UtcNow.Date;
            return GetSupervisorTripsAsync(supervisorId, today, today);
        }

    }
}
