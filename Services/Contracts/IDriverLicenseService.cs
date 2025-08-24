using Services.Models.Driver;

namespace Services.Contracts
{
    public interface IDriverLicenseService
    {
        Task<DriverLicenseResponse> CreateDriverLicenseAsync(CreateDriverLicenseRequest request);
        Task<DriverLicenseResponse?> GetDriverLicenseByDriverIdAsync(Guid driverId);
        Task<DriverLicenseResponse> UpdateDriverLicenseAsync(Guid id, CreateDriverLicenseRequest request);
        Task<bool> DeleteDriverLicenseAsync(Guid id);
    }
}
