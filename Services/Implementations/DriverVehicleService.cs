using AutoMapper;
using Data.Models;
using Data.Repos.Interfaces;
using Services.Contracts;
using Services.Models.DriverVehicle;

namespace Services.Implementations
{
    public class DriverVehicleService : IDriverVehicleService
    {
        private readonly IDriverVehicleRepository _driverVehicleRepo;
        private readonly IVehicleRepository _vehicleRepo;
        private readonly IMapper _mapper;

        public DriverVehicleService(
            IDriverVehicleRepository driverVehicleRepo,
            IVehicleRepository vehicleRepo,
            IMapper mapper)
        {
            _driverVehicleRepo = driverVehicleRepo;
            _vehicleRepo = vehicleRepo;
            _mapper = mapper;
        }

        public async Task<VehicleDriversResponse?> GetDriversByVehicleAsync(Guid vehicleId, bool? isActive)
        {
            var vehicle = await _vehicleRepo.FindAsync(vehicleId);
            if (vehicle == null || vehicle.IsDeleted) return null;

            var assignments = await _driverVehicleRepo.GetByVehicleIdAsync(vehicleId, isActive);

            return new VehicleDriversResponse
            {
                Success = true,
                Data = assignments.Select(_mapper.Map<DriverAssignmentDto>).ToList()
            };
        }

        public async Task<DriverAssignmentResponse?> AssignDriverAsync(Guid vehicleId, DriverAssignmentRequest dto, Guid adminId)
        {
            var vehicle = await _vehicleRepo.FindAsync(vehicleId);
            if (vehicle == null || vehicle.IsDeleted) return null;

            if (dto.EndTimeUtc.HasValue && dto.EndTimeUtc <= dto.StartTimeUtc)
                throw new InvalidOperationException("End time cannot be earlier than start time.");

            var alreadyAssigned = await _driverVehicleRepo.IsDriverAlreadyAssignedAsync(vehicleId, dto.DriverId, true);
            if (alreadyAssigned)
                throw new InvalidOperationException("This driver is already assigned to the vehicle");

            var entity = new DriverVehicle
            {
                DriverId = dto.DriverId,
                VehicleId = vehicleId,
                IsPrimaryDriver = dto.IsPrimaryDriver,
                StartTimeUtc = dto.StartTimeUtc,
                EndTimeUtc = dto.EndTimeUtc
            };

            var created = await _driverVehicleRepo.AssignDriverAsync(entity);

            var dtoResult = _mapper.Map<DriverAssignmentDto>(created);
            dtoResult.AssignedByAdminId = adminId; 

            return new DriverAssignmentResponse
            {
                Success = true,
                Data = dtoResult
            };
        }

    }
}
