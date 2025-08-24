using Microsoft.AspNetCore.Http;

namespace Services.Contracts
{
    public interface IFileService
    {
        Task<Guid> UploadUserPhotoAsync(Guid userId, IFormFile file);
        Task<Guid> UploadHealthCertificateAsync(Guid driverId, IFormFile file);
        Task<Guid> UploadLicenseImageAsync(Guid driverLicenseId, IFormFile file);
        Task<byte[]> GetFileAsync(Guid fileId);
        Task<bool> DeleteFileAsync(Guid fileId);
        Task<string> GetFileContentTypeAsync(Guid fileId);
    }
}
