using AutoMapper;
using Constants;
using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using Microsoft.AspNetCore.Http;
using Services.Contracts;
using Services.Models.Common;
using Services.Models.StudentAbsenceRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using Utils;

namespace Services.Implementations
{
    public class StudentAbsenceRequestService : IStudentAbsenceRequestService
    {
        private readonly IStudentAbsenceRequestRepository _repository;
        private readonly ITripRepository _tripRepository;
        private readonly IStudentService _studentService;
        private readonly IParentRepository _parentRepository;
        private readonly IMapper _mapper;

        public StudentAbsenceRequestService(
            IStudentAbsenceRequestRepository repository,
            ITripRepository tripRepository,
            IStudentService studentService,
            IParentRepository parentRepository,
            IMapper mapper)
        {
            _repository = repository;
            _tripRepository = tripRepository;
            _studentService = studentService;
            _parentRepository = parentRepository;
            _mapper = mapper;
        }

        public async Task<StudentAbsenceRequestResponseDto> CreateAsync(CreateStudentAbsenceRequestDto createDto, HttpContext httpContext)
        {
            var student = await _studentService.GetStudentByIdAsync(createDto.StudentId);
            if (student is null)
                throw new KeyNotFoundException("Student not found.");

            if (!student.ParentId.HasValue)
                throw new ArgumentException("Student has not been assigned to a parent yet.");

            if (!AuthorizationHelper.CanAccessStudentData(httpContext, student.ParentId))
                throw new UnauthorizedAccessException("You are not allowed to request absence for this student.");

            var parentId = student.ParentId.Value;
            createDto.ParentId = parentId;

            var today = DateTime.UtcNow.Date;
            var normalizedStart = createDto.StartDate.Date;

            if (normalizedStart < today)
                throw new ArgumentException("Start date must be today or later.");

            var normalizedEnd = createDto.EndDate.Date;
            if (normalizedEnd < normalizedStart)
                throw new ArgumentException("End date must be greater than or equal to start date.");

            normalizedEnd = normalizedEnd.AddDays(1).AddTicks(-1);

            createDto.StartDate = normalizedStart;
            createDto.EndDate = normalizedEnd;

            var studentHasTrips = await _tripRepository.StudentHasTripsBetweenDatesAsync(
                createDto.StudentId,
                normalizedStart,
                normalizedEnd);

            if (!studentHasTrips)
                throw new ArgumentException("Student has no scheduled trips in the selected period.");

            var overlap = await _repository.GetPendingOverlapAsync(createDto.StudentId, createDto.StartDate, createDto.EndDate);
            if (overlap is not null)
                throw new InvalidOperationException("Student already has a pending absence request overlapping this period.");

            var hasApprovedExactMatch = await _repository.HasApprovedRequestWithExactRangeAsync(
                createDto.StudentId,
                createDto.StartDate,
                createDto.EndDate);

            if (hasApprovedExactMatch)
                throw new InvalidOperationException("Student already has an approved absence request for the selected period.");

            var parent = await _parentRepository.FindAsync(parentId);
            if (parent is null)
                throw new InvalidOperationException("Parent account not found.");

            var entity = _mapper.Map<StudentAbsenceRequest>(createDto);
            entity.StudentName = BuildStudentFullName(student.FirstName, student.LastName);
            entity.ParentName = BuildFullName(parent.FirstName, parent.LastName, "Parent");
            entity.ParentEmail = parent.Email ?? string.Empty;
            entity.ParentPhoneNumber = parent.PhoneNumber ?? string.Empty;

            await _repository.AddAsync(entity);
            return _mapper.Map<StudentAbsenceRequestResponseDto>(entity);
        }

        private static string BuildStudentFullName(string firstName, string lastName) =>
            BuildFullName(firstName, lastName, "Student");

        private static string BuildFullName(string? firstName, string? lastName, string defaultName)
        {
            var trimmedFirst = firstName?.Trim();
            var trimmedLast = lastName?.Trim();

            if (!string.IsNullOrWhiteSpace(trimmedFirst) && !string.IsNullOrWhiteSpace(trimmedLast))
            {
                return $"{trimmedFirst} {trimmedLast}";
            }

            if (!string.IsNullOrWhiteSpace(trimmedFirst))
            {
                return trimmedFirst;
            }

            if (!string.IsNullOrWhiteSpace(trimmedLast))
            {
                return trimmedLast;
            }

            return defaultName;
        }

        public async Task<StudentAbsenceRequestListResponse> GetByStudentAsync(
            Guid studentId,
            DateTime? startDate,
            DateTime? endDate,
            AbsenceRequestStatus? status,
            CreateAtSortOption sort,
            int page,
            int perPage)
        {
            if (page < 1)
                page = 1;

            if (perPage < 1 || perPage > 100)
                perPage = 20;

            if (startDate.HasValue && endDate.HasValue && startDate.Value.Date > endDate.Value.Date)
                throw new ArgumentException("Start date cannot be later than end date.");

            var normalizedStart = startDate?.Date;
            DateTime? normalizedEnd = null;
            if (endDate.HasValue)
                normalizedEnd = endDate.Value.Date.AddDays(1).AddTicks(-1);

            var (entities, totalCount) = await _repository.GetByStudentAsync(
                studentId,
                normalizedStart,
                normalizedEnd,
                status,
                sort,
                page,
                perPage);

            var totalPages = (int)Math.Ceiling((double)totalCount / perPage);

            return new StudentAbsenceRequestListResponse
            {
                Data = _mapper.Map<List<StudentAbsenceRequestListItemDto>>(entities),
                Pagination = new PaginationInfo
                {
                    CurrentPage = page,
                    PerPage = perPage,
                    TotalItems = totalCount,
                    TotalPages = totalPages,
                    HasNextPage = page < totalPages,
                    HasPreviousPage = page > 1
                }
            };
        }

        public async Task<StudentAbsenceRequestListResponse> GetByParentAsync(
            Guid parentId,
            DateTime? startDate,
            DateTime? endDate,
            AbsenceRequestStatus? status,
            CreateAtSortOption sort,
            int page,
            int perPage)
        {
            if (page < 1)
                page = 1;

            if (perPage < 1 || perPage > 100)
                perPage = 20;

            if (startDate.HasValue && endDate.HasValue && startDate.Value.Date > endDate.Value.Date)
                throw new ArgumentException("Start date cannot be later than end date.");

            var normalizedStart = startDate?.Date;
            DateTime? normalizedEnd = null;
            if (endDate.HasValue)
                normalizedEnd = endDate.Value.Date.AddDays(1).AddTicks(-1);

            var (entities, totalCount) = await _repository.GetByParentAsync(
                parentId,
                normalizedStart,
                normalizedEnd,
                status,
                sort,
                page,
                perPage);

            var totalPages = (int)Math.Ceiling((double)totalCount / perPage);

            return new StudentAbsenceRequestListResponse
            {
                Data = _mapper.Map<List<StudentAbsenceRequestListItemDto>>(entities),
                Pagination = new PaginationInfo
                {
                    CurrentPage = page,
                    PerPage = perPage,
                    TotalItems = totalCount,
                    TotalPages = totalPages,
                    HasNextPage = page < totalPages,
                    HasPreviousPage = page > 1
                }
            };
        }

        public async Task<StudentAbsenceRequestListResponse> GetAllAsync(
            DateTime? startDate,
            DateTime? endDate,
            AbsenceRequestStatus? status,
            string? studentName,
            CreateAtSortOption sort,
            int page,
            int perPage)
        {
            if (page < 1)
                page = 1;

            if (perPage < 1 || perPage > 100)
                perPage = 20;

            if (startDate.HasValue && endDate.HasValue && startDate.Value.Date > endDate.Value.Date)
                throw new ArgumentException("Start date cannot be later than end date.");

            var normalizedStart = startDate?.Date;
            DateTime? normalizedEnd = null;
            if (endDate.HasValue)
                normalizedEnd = endDate.Value.Date.AddDays(1).AddTicks(-1);

            var normalizedSearch = string.IsNullOrWhiteSpace(studentName)
                ? null
                : studentName!.Trim();

            var (entities, totalCount) = await _repository.GetAllAsync(
                normalizedStart,
                normalizedEnd,
                status,
                normalizedSearch,
                sort,
                page,
                perPage);

            var totalPages = (int)Math.Ceiling((double)totalCount / perPage);

            return new StudentAbsenceRequestListResponse
            {
                Data = _mapper.Map<List<StudentAbsenceRequestListItemDto>>(entities),
                Pagination = new PaginationInfo
        {
                    CurrentPage = page,
                    PerPage = perPage,
                    TotalItems = totalCount,
                    TotalPages = totalPages,
                    HasNextPage = page < totalPages,
                    HasPreviousPage = page > 1
                }
            };
        }

        public async Task<StudentAbsenceRequestResponseDto?> GetByIdAsync(Guid requestId)
        {
            var entity = await _repository.FindAsync(requestId);
            if (entity == null || entity.IsDeleted)
                return null;

            return _mapper.Map<StudentAbsenceRequestResponseDto>(entity);
        }

        public Task<StudentAbsenceRequest?> GetPendingOverlapAsync(Guid studentId, DateTime start, DateTime end) =>
            _repository.GetPendingOverlapAsync(studentId, start, end);

        public async Task<StudentAbsenceRequestResponseDto?> UpdateStatusAsync(UpdateStudentAbsenceStatusDto updateDto)
        {
            var entity = await _repository.FindAsync(updateDto.RequestId);
            if (entity is null || entity.IsDeleted)
                return null;

            _mapper.Map(updateDto, entity);

            await _repository.UpdateAsync(entity);
            return _mapper.Map<StudentAbsenceRequestResponseDto>(entity);
        }

        public async Task<StudentAbsenceRequestResponseDto> RejectRequestAsync(Guid requestId, RejectStudentAbsenceRequestDto dto, Guid adminId)
        {
            var entity = await _repository.FindAsync(requestId);
            if (entity == null || entity.IsDeleted)
                throw new InvalidOperationException("Absence request not found.");

            if (entity.Status != AbsenceRequestStatus.Pending)
            {
                throw new InvalidOperationException($"Cannot reject absence request. Current status: {entity.Status}. Only pending absence requests can be rejected.");
            }

            entity.Status = AbsenceRequestStatus.Rejected;
            entity.ReviewedBy = adminId;
            entity.ReviewedAt = DateTime.UtcNow;
            entity.Notes = dto.Reason;
            entity.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(entity);
            return _mapper.Map<StudentAbsenceRequestResponseDto>(entity);
        }

        public async Task<StudentAbsenceRequestResponseDto> ApproveRequestAsync(Guid requestId, ApproveStudentAbsenceRequestDto dto, Guid adminId)
        {
            dto ??= new ApproveStudentAbsenceRequestDto();

            var entity = await _repository.FindAsync(requestId);
            if (entity == null || entity.IsDeleted)
                throw new InvalidOperationException("Absence request not found.");

            if (entity.Status != AbsenceRequestStatus.Pending)
                throw new InvalidOperationException($"Cannot approve absence request. Current status: {entity.Status}. Only pending absence requests can be approved.");

            var trips = (await _tripRepository.GetTripsByStudentAndDateRangeAsync(
                entity.StudentId,
                entity.StartDate,
                entity.EndDate))?.ToList();

            if (trips == null || trips.Count == 0)
                throw new InvalidOperationException("Student has no scheduled trips within the requested absence period.");

			await MarkStudentAbsentInTripsAsync(entity.StudentId, trips);

			entity.Status = AbsenceRequestStatus.Approved;
            entity.ReviewedBy = adminId;
            entity.ReviewedAt = DateTime.UtcNow;
            entity.Notes = dto.Notes;
            entity.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(entity);
            return _mapper.Map<StudentAbsenceRequestResponseDto>(entity);
        }

		private async Task MarkStudentAbsentInTripsAsync(Guid studentId, IEnumerable<Trip> trips)
		{
			if (trips == null)
				return;

			var tripsToUpdate = new List<Trip>();

			foreach (var trip in trips)
			{
				if (trip.Stops == null || trip.Stops.Count == 0)
					continue;

				var updated = false;

				foreach (var stop in trip.Stops)
				{
					if (stop.Attendance == null || stop.Attendance.Count == 0)
						continue;

					// Find the student in the attendance list
					var attendanceRecord = stop.Attendance.FirstOrDefault(a => a.StudentId == studentId);

					// If found, mark as Absent and clear boarding info
					if (attendanceRecord != null)
					{
						attendanceRecord.State = TripConstants.AttendanceStates.Absent;
						attendanceRecord.BoardedAt = null;
						attendanceRecord.AlightedAt = null;
						attendanceRecord.BoardStatus = null;
						attendanceRecord.AlightStatus = null;
						updated = true;
					}
				}

				if (updated)
				{
					tripsToUpdate.Add(trip);
				}
			}

			if (tripsToUpdate.Any())
			{
				await _tripRepository.BulkUpdateAsync(tripsToUpdate);
			}
		}
	}
}