using AutoMapper;
using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using Services.Contracts;
using Services.Models.Vehicle;
using Utils;

namespace Services.Implementations
{
    public class VehicleService : IVehicleService
    {
        private readonly IVehicleRepository _vehicleRepo;
		private readonly IMongoRepository<Route> _routeRepository;
		private readonly IMapper _mapper;

        public VehicleService(IVehicleRepository vehicleRepo, IMongoRepository<Route> routeRepository, IMapper mapper)
        {
            _vehicleRepo = vehicleRepo;
			_routeRepository = routeRepository;
			_mapper = mapper;
        }
        public async Task<VehicleListResponse> GetVehiclesAsync(
            string? status, int? capacity, Guid? adminId, string? search,
            int page, int perPage, string? sortBy, string sortOrder)
        {
            var query = _vehicleRepo.GetQueryable().Where(v => !v.IsDeleted);

            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<VehicleStatus>(status, true, out var st))
            {
                query = query.Where(v => v.Status == st);
            }
            if (capacity.HasValue) query = query.Where(v => v.Capacity == capacity.Value);
            if (adminId.HasValue) query = query.Where(v => v.AdminId == adminId.Value);

            // sorting
            var direction = string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";
            var sortable = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "capacity", "status", "createdAt", "updatedAt" };
            var column = sortable.Contains(sortBy ?? "") ? sortBy! : "createdAt";
            query = query.OrderBy($"{column} {direction}");

            // search 
            if (!string.IsNullOrWhiteSpace(search))
            {
                var needle = Normalize(search);
                // because LicensePlate is stored encrypted, project and decrypt to filter
                var all = await query
                    .Select(v => new { V = v, Plate = SecurityHelper.DecryptFromBytes(v.HashedLicensePlate) })
                    .ToListAsync();

                var filtered = all
                    .Where(x => Normalize(x.Plate).Contains(needle))
                    .Select(x => x.V)
                    .ToList();

                var totalCount = filtered.Count;
                var pageItems = filtered
                    .Skip((page - 1) * perPage)
                    .Take(perPage)
                    .ToList();

                var dtos = pageItems.Select(v =>
                {
                    var dto = _mapper.Map<VehicleDto>(v);
                    dto.LicensePlate = SecurityHelper.DecryptFromBytes(v.HashedLicensePlate);
                    return dto;
                }).ToList();

                return new VehicleListResponse
                {
                    Vehicles = dtos,
                    TotalCount = totalCount,
                    Page = page,
                    PerPage = perPage,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)perPage)
                };
            }

            // no search: paginate from DB
            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * perPage)
                .Take(perPage)
                .ToListAsync();

            var list = items.Select(v =>
            {
                var dto = _mapper.Map<VehicleDto>(v);
                dto.LicensePlate = SecurityHelper.DecryptFromBytes(v.HashedLicensePlate);
                return dto;
            }).ToList();

            return new VehicleListResponse
            {
                Vehicles = list,
                TotalCount = total,
                Page = page,
                PerPage = perPage,
                TotalPages = (int)Math.Ceiling(total / (double)perPage)
            };

        }

        // Helper method to normalize license plate for comparison
        private static string Normalize(string s) =>
            new string((s ?? "").Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

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
            // Check for duplicate license plate
            var existingVehicles = await _vehicleRepo.GetQueryable()
                .Where(v => !v.IsDeleted)
                .ToListAsync();

            var normalizedNewPlate = Normalize(dto.LicensePlate);
            var isDuplicate = existingVehicles.Any(v => 
                Normalize(SecurityHelper.DecryptFromBytes(v.HashedLicensePlate)) == normalizedNewPlate);

            if (isDuplicate)
            {
                return new VehicleResponse
                {
                    Success = false,
                    Error = "LICENSE_PLATE_ALREADY_EXISTS",
                    Message = $"Vehicle with license plate '{dto.LicensePlate}' already exists."
                };
            }

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

            // Check for duplicate license plate (excluding current vehicle)
            var existingVehicles = await _vehicleRepo.GetQueryable()
                .Where(v => !v.IsDeleted && v.Id != id)
                .ToListAsync();

            var normalizedNewPlate = Normalize(dto.LicensePlate);
            var isDuplicate = existingVehicles.Any(v => 
                Normalize(SecurityHelper.DecryptFromBytes(v.HashedLicensePlate)) == normalizedNewPlate);

            if (isDuplicate)
            {
                return new VehicleResponse
                {
                    Success = false,
                    Error = "LICENSE_PLATE_ALREADY_EXISTS",
                    Message = $"Vehicle with license plate '{dto.LicensePlate}' already exists."
                };
            }

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

            // Check for duplicate license plate if it's being updated
            if (!string.IsNullOrEmpty(dto.LicensePlate))
            {
                var existingVehicles = await _vehicleRepo.GetQueryable()
                    .Where(v => !v.IsDeleted && v.Id != id)
                    .ToListAsync();

                var normalizedNewPlate = Normalize(dto.LicensePlate);
                var isDuplicate = existingVehicles.Any(v => 
                    Normalize(SecurityHelper.DecryptFromBytes(v.HashedLicensePlate)) == normalizedNewPlate);

                if (isDuplicate)
                {
                    return new VehicleResponse
                    {
                        Success = false,
                        Error = "LICENSE_PLATE_ALREADY_EXISTS",
                        Message = $"Vehicle with license plate '{dto.LicensePlate}' already exists."
                    };
                }

                vehicle.HashedLicensePlate = SecurityHelper.EncryptToBytes(dto.LicensePlate);
            }

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

		public async Task<VehicleListResponse> GetUnassignedVehiclesAsync(Guid? excludeRouteId = null)
		{
			// Get all vehicle IDs that are assigned to active routes
			var activeRoutes = await _routeRepository.FindByConditionAsync(r =>
				!r.IsDeleted && r.IsActive);

			var assignedVehicleIds = activeRoutes
		        .Where(r => !excludeRouteId.HasValue || r.Id != excludeRouteId.Value) // Exclude the specified route
		        .Select(r => r.VehicleId)
		        .Distinct()
		        .ToList();

			// Get vehicles that are not assigned to any route
			var vehicles = await _vehicleRepo.GetVehiclesAsync(exceptionIds: assignedVehicleIds);

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
	}
}
