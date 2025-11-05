using AutoMapper;
using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using Services.Contracts;
using Services.Extensions;
using Services.Models.Vehicle;
using Services.Validators;
using Utils;

namespace Services.Implementations
{
    public class VehicleService : IVehicleService
    {
        private readonly IVehicleRepository _vehicleRepo;
		private readonly IMongoRepository<Route> _routeRepository;
		private readonly IMapper _mapper;
		private readonly LicensePlateValidator _licensePlateValidator;

        public VehicleService(IVehicleRepository vehicleRepo, IMongoRepository<Route> routeRepository, IMapper mapper, LicensePlateValidator licensePlateValidator)
        {
            _vehicleRepo = vehicleRepo;
			_routeRepository = routeRepository;
			_mapper = mapper;
			_licensePlateValidator = licensePlateValidator;
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

                var dtos = pageItems.ToDecryptedDtos(_mapper).ToList();

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

            var list = items.ToDecryptedDtos(_mapper).ToList();

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

            var dto = vehicle.ToDecryptedDto(_mapper);

            return new VehicleResponse
            {
                Success = true,
                Data = dto
            };
        }

        public async Task<VehicleResponse> CreateAsync(VehicleCreateRequest dto, Guid adminId)
        {
            // Check for duplicate license plate
            if (await _licensePlateValidator.IsDuplicateAsync(dto.LicensePlate))
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

            var resultDto = vehicle.ToDecryptedDto(_mapper);

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
            if (await _licensePlateValidator.IsDuplicateAsync(dto.LicensePlate, id))
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

            var resultDto = vehicle.ToDecryptedDto(_mapper);

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
                if (await _licensePlateValidator.IsDuplicateAsync(dto.LicensePlate, id))
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

            var resultDto = vehicle.ToDecryptedDto(_mapper);

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

			var dtos = vehicles.ToDecryptedDtos(_mapper).ToList();

		return new VehicleListResponse
		{
			Vehicles = dtos,
			TotalCount = dtos.Count,
			Page = 1,
			PerPage = dtos.Count,
			TotalPages = 1
		};
		}
	}
}
