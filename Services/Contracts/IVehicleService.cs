using Services.Models.Vehicle;

namespace Services.Contracts
{
    public interface IVehicleService
    {
        Task<VehicleListResponse> GetVehiclesAsync(
            string? status,
            int? capacity,
            Guid? adminId,
            int page,
            int perPage,
            string? sortBy,
            string sortOrder);

        Task<VehicleResponse?> GetByIdAsync(Guid id);
        Task<VehicleResponse> CreateAsync(VehicleCreateRequest dto, Guid adminId);
        Task<VehicleResponse?> UpdateAsync(Guid id, VehicleUpdateRequest dto);
        Task<VehicleResponse?> PartialUpdateAsync(Guid id, VehiclePartialUpdateRequest dto);
        Task<bool> DeleteAsync(Guid id);
    }
}
