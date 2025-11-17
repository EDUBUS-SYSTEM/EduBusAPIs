using AutoMapper;
using Data.Models;
using Data.Repos.Interfaces;
using Services.Contracts;
using Services.Models.StudentAbsenceRequest;
using System;

namespace Services.Implementations
{
    public class StudentAbsenceRequestService : IStudentAbsenceRequestService
    {
        private readonly IStudentAbsenceRequestRepository _repository;
        private readonly ITripRepository _tripRepository;
        private readonly IMapper _mapper;

        public StudentAbsenceRequestService(
            IStudentAbsenceRequestRepository repository,
            ITripRepository tripRepository,
            IMapper mapper)
        {
            _repository = repository;
            _tripRepository = tripRepository;
            _mapper = mapper;
        }

        public async Task<StudentAbsenceRequestResponseDto> CreateAsync(CreateStudentAbsenceRequestDto createDto)
        {
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

            var entity = _mapper.Map<StudentAbsenceRequest>(createDto);

            await _repository.AddAsync(entity);
            return _mapper.Map<StudentAbsenceRequestResponseDto>(entity);
        }

        public async Task<IEnumerable<StudentAbsenceRequestResponseDto>> GetByStudentAsync(Guid studentId)
        {
            var entities = await _repository.GetByStudentAsync(studentId);
            return _mapper.Map<IEnumerable<StudentAbsenceRequestResponseDto>>(entities);
        }

        public async Task<IEnumerable<StudentAbsenceRequestResponseDto>> GetByParentAsync(Guid parentId)
        {
            var entities = await _repository.GetByParentAsync(parentId);
            return _mapper.Map<IEnumerable<StudentAbsenceRequestResponseDto>>(entities);
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
    }
}