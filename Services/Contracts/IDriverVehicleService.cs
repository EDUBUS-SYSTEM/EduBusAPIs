using Services.Models.DriverVehicle;

namespace Services.Contracts
{
    public interface IDriverVehicleService
    {
        Task<VehicleDriversResponse?> GetDriversByVehicleAsync(Guid vehicleId, bool? isActive);
        Task<DriverAssignmentResponse?> AssignDriverAsync(Guid vehicleId, DriverAssignmentRequest dto, Guid adminId);
    }
}
