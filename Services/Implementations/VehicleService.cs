using AutoMapper;
using Data.Models;
using Data.Repos.Interfaces;
using Services.Contracts;
using Services.Models.Vehicle;
using Utils;

namespace Services.Implementations
{
    public class VehicleService : IVehicleService
    {
        private readonly IVehicleRepository _vehicleRepo;
        private readonly IMapper _mapper;

        public VehicleService(IVehicleRepository vehicleRepo, IMapper mapper)
        {
            _vehicleRepo = vehicleRepo;
            _mapper = mapper;
        }

        public async Task<VehicleListResponse> GetVehiclesAsync(string? status, int? capacity, Guid? adminId,
            int page, int perPage, string? sortBy, string sortOrder)
        {
            var vehicles = await _vehicleRepo.GetVehiclesAsync(status, capacity, adminId, page, perPage, sortBy, sortOrder);

            var dtos = vehicles.Select(v =>
            {
                var dto = _mapper.Map<VehicleDto>(v);
                dto.LicensePlate = SecurityHelper.DecryptFromBytes(v.HashedLicensePlate); 
                return dto;
            }).ToList();

            return new VehicleListResponse
            {
                Success = true,
                Data = dtos
            };
        }

        public async Task<VehicleResponse?> GetByIdAsync(Guid id)
        {
            var vehicle = await _vehicleRepo.FindAsync(id);
            if (vehicle == null || vehicle.IsDeleted) return null;

            var dto = _mapper.Map<VehicleDto>(vehicle);
            dto.LicensePlate = SecurityHelper.DecryptFromBytes(vehicle.HashedLicensePlate);

            return new VehicleResponse
            {
                Success = true,
                Data = dto
            };
        }

        public async Task<VehicleResponse> CreateAsync(VehicleCreateRequest dto, Guid adminId)
        {
            var vehicle = new Vehicle
            {
                Id = Guid.NewGuid(),
                HashedLicensePlate = SecurityHelper.EncryptToBytes(dto.LicensePlate),
                Capacity = (int)dto.Capacity,        
                Status = dto.Status,       
                AdminId = adminId,
                CreatedAt = DateTime.UtcNow
            };

            await _vehicleRepo.AddAsync(vehicle);

            var resultDto = _mapper.Map<VehicleDto>(vehicle);
            resultDto.LicensePlate = dto.LicensePlate;

            return new VehicleResponse
            {
                Success = true,
                Data = resultDto
            };
        }

        public async Task<VehicleResponse?> UpdateAsync(Guid id, VehicleUpdateRequest dto)
        {
            var vehicle = await _vehicleRepo.FindAsync(id);
            if (vehicle == null || vehicle.IsDeleted) return null;

            vehicle.HashedLicensePlate = SecurityHelper.EncryptToBytes(dto.LicensePlate);
            vehicle.Capacity = (int)dto.Capacity;    
            vehicle.Status = dto.Status;  
            vehicle.UpdatedAt = DateTime.UtcNow;

            await _vehicleRepo.UpdateAsync(vehicle);

            var resultDto = _mapper.Map<VehicleDto>(vehicle);
            resultDto.LicensePlate = dto.LicensePlate;

            return new VehicleResponse
            {
                Success = true,
                Data = resultDto
            };
        }

        public async Task<VehicleResponse?> PartialUpdateAsync(Guid id, VehiclePartialUpdateRequest dto)
        {
            var vehicle = await _vehicleRepo.FindAsync(id);
            if (vehicle == null || vehicle.IsDeleted) return null;

            if (!string.IsNullOrEmpty(dto.LicensePlate))
                vehicle.HashedLicensePlate = SecurityHelper.EncryptToBytes(dto.LicensePlate);

            if (dto.Capacity.HasValue)
                vehicle.Capacity = (int)dto.Capacity.Value;

            if (dto.Status.HasValue)
                vehicle.Status = dto.Status.Value;

            vehicle.UpdatedAt = DateTime.UtcNow;
            await _vehicleRepo.UpdateAsync(vehicle);

            var resultDto = _mapper.Map<VehicleDto>(vehicle);
            resultDto.LicensePlate = !string.IsNullOrEmpty(dto.LicensePlate)
                ? dto.LicensePlate
                : SecurityHelper.DecryptFromBytes(vehicle.HashedLicensePlate);

            return new VehicleResponse
            {
                Success = true,
                Data = resultDto
            };
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var vehicle = await _vehicleRepo.FindAsync(id);
            if (vehicle == null || vehicle.IsDeleted) return false;

            await _vehicleRepo.DeleteAsync(vehicle);
            return true;
        }
    }
}
