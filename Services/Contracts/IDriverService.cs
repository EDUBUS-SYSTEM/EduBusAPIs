using Services.Models.Driver;
using Services.Models.UserAccount;
using Data.Models.Enums;

namespace Services.Contracts
{
    public interface IDriverService
    {
        Task<CreateUserResponse> CreateDriverAsync(CreateDriverRequest dto);
        Task<ImportUsersResult> ImportDriversFromExcelAsync(Stream excelFileStream);
        Task<byte[]> ExportDriversToExcelAsync();
        Task<Data.Models.Driver?> GetDriverByIdAsync(Guid driverId);
        Task<Guid?> GetHealthCertificateFileIdAsync(Guid driverId);
        Task<IEnumerable<DriverResponse>> GetAllDriversAsync();
        Task<DriverResponse?> GetDriverResponseByIdAsync(Guid driverId);
        
        Task<DriverResponse> UpdateDriverStatusAsync(Guid driverId, DriverStatus status, string? note);
        Task<DriverResponse> SuspendDriverAsync(Guid driverId, string reason, DateTime? untilDate);
        Task<DriverResponse> ReactivateDriverAsync(Guid driverId);
        Task<IEnumerable<DriverResponse>> GetDriversByStatusAsync(DriverStatus status);
        
        Task<bool> IsDriverAvailableAsync(Guid driverId, DateTime startTime, DateTime endTime);
        Task<IEnumerable<DriverResponse>> GetAvailableDriversAsync(DateTime startTime, DateTime endTime);
    }
}
