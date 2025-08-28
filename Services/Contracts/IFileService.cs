using Microsoft.AspNetCore.Http;
using Data.Models;

namespace Services.Contracts
{
    public interface IFileService
    {
        Task<Guid> UploadFileAsync(Guid entityId, string entityType, string fileType, IFormFile file);
        Task<Guid> UploadUserPhotoAsync(Guid userId, IFormFile file);
        Task<Guid> UploadHealthCertificateAsync(Guid driverId, IFormFile file);
        Task<Guid> UploadLicenseImageAsync(Guid driverLicenseId, IFormFile file);
        Task<byte[]> GetFileAsync(Guid fileId);
        Task<bool> DeleteFileAsync(Guid fileId);
        Task<string> GetFileContentTypeAsync(Guid fileId);
        Task<Guid?> GetUserPhotoFileIdAsync(Guid userId);
        Task<FileStorage?> GetTemplateFileAsync(string templateType);
    }
}
