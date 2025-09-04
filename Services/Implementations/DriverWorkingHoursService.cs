using AutoMapper;
using Data.Models;
using Data.Repos.Interfaces;
using Services.Contracts;
using Services.Models.Driver;

namespace Services.Implementations
{
    public class DriverWorkingHoursService : IDriverWorkingHoursService
    {
        private readonly IDriverWorkingHoursRepository _repo;
        private readonly IMapper _mapper;

        public DriverWorkingHoursService(IDriverWorkingHoursRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<DriverWorkingHoursResponse> CreateWorkingHoursAsync(CreateWorkingHoursDto dto)
        {
            var entity = _mapper.Map<DriverWorkingHours>(dto);
            entity.Id = Guid.NewGuid();
            var created = await _repo.AddAsync(entity);
            return _mapper.Map<DriverWorkingHoursResponse>(created);
        }

        public async Task<DriverWorkingHoursResponse> UpdateWorkingHoursAsync(Guid workingHoursId, UpdateWorkingHoursDto dto)
        {
            var entity = await _repo.FindAsync(workingHoursId) ?? throw new InvalidOperationException("Working hours not found");
            if (dto.StartTime.HasValue) entity.StartTime = dto.StartTime.Value;
            if (dto.EndTime.HasValue) entity.EndTime = dto.EndTime.Value;
            if (dto.IsAvailable.HasValue) entity.IsAvailable = dto.IsAvailable.Value;
            entity.UpdatedAt = DateTime.UtcNow;
            var updated = await _repo.UpdateAsync(entity);
            return _mapper.Map<DriverWorkingHoursResponse>(updated!);
        }

        public async Task<bool> DeleteWorkingHoursAsync(Guid workingHoursId)
        {
            var entity = await _repo.FindAsync(workingHoursId);
            if (entity == null) return false;
            await _repo.DeleteAsync(entity);
            return true;
        }

        public Task<bool> IsDriverAvailableAtTimeAsync(Guid driverId, DateTime dateTime)
        {
            return _repo.IsDriverAvailableAtTimeAsync(driverId, dateTime);
        }

        public async Task<IEnumerable<DriverResponse>> GetAvailableDriversAtTimeAsync(DateTime dateTime)
        {
            var working = await _repo.GetAvailableDriversAtTimeAsync(dateTime);
            // Only return driver IDs; mapping to DriverResponse would need driver repo; keeping minimal
            return working.Select(w => new DriverResponse { Id = w.DriverId });
        }

        public async Task<IEnumerable<DriverResponse>> GetDriversAvailableInTimeRangeAsync(DateTime startTime, DateTime endTime)
        {
            var working = await _repo.GetDriversAvailableInTimeRangeAsync(startTime, endTime);
            return working.Select(w => new DriverResponse { Id = w.DriverId });
        }

        public async Task<IEnumerable<DriverWorkingHoursResponse>> GetWorkingHoursByDriverAsync(Guid driverId)
        {
            var list = await _repo.GetByDriverIdAsync(driverId);
            return list.Select(_mapper.Map<DriverWorkingHoursResponse>);
        }

        public async Task<DriverWorkingHoursResponse?> GetWorkingHoursByDriverAndDayAsync(Guid driverId, DayOfWeek dayOfWeek)
        {
            var entity = await _repo.GetByDriverAndDayAsync(driverId, dayOfWeek);
            return entity == null ? null : _mapper.Map<DriverWorkingHoursResponse>(entity);
        }

        public async Task<IEnumerable<DriverWorkingHoursResponse>> SetDefaultWorkingHoursAsync(Guid driverId)
        {
            var defaults = new List<DriverWorkingHoursResponse>();
            var days = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
            foreach (var day in days)
            {
                var entity = new DriverWorkingHours
                {
                    Id = Guid.NewGuid(),
                    DriverId = driverId,
                    DayOfWeek = day,
                    StartTime = new TimeSpan(6, 0, 0),
                    EndTime = new TimeSpan(18, 0, 0),
                    IsAvailable = true
                };
                var created = await _repo.AddAsync(entity);
                defaults.Add(_mapper.Map<DriverWorkingHoursResponse>(created));
            }
            return defaults;
        }

        public async Task<IEnumerable<DriverWorkingHoursResponse>> CopyWorkingHoursFromDriverAsync(Guid sourceDriverId, Guid targetDriverId)
        {
            var source = await _repo.GetByDriverIdAsync(sourceDriverId);
            var results = new List<DriverWorkingHoursResponse>();
            foreach (var s in source)
            {
                var clone = new DriverWorkingHours
                {
                    Id = Guid.NewGuid(),
                    DriverId = targetDriverId,
                    DayOfWeek = s.DayOfWeek,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    IsAvailable = s.IsAvailable
                };
                var created = await _repo.AddAsync(clone);
                results.Add(_mapper.Map<DriverWorkingHoursResponse>(created));
            }
            return results;
        }
    }
}
