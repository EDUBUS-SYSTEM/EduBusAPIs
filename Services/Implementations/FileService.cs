using Data.Models;
using Data.Repos.Interfaces;
using Microsoft.AspNetCore.Http;
using Services.Contracts;

namespace Services.Implementations
{
    public class FileService : IFileService
    {
        private readonly IFileStorageRepository _fileStorageRepository;
        private readonly IDriverRepository _driverRepository;
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IDriverLicenseRepository _driverLicenseRepository;

        public FileService(
            IFileStorageRepository fileStorageRepository,
            IDriverRepository driverRepository,
            IUserAccountRepository userAccountRepository,
            IDriverLicenseRepository driverLicenseRepository)
        {
            _fileStorageRepository = fileStorageRepository;
            _driverRepository = driverRepository;
            _userAccountRepository = userAccountRepository;
            _driverLicenseRepository = driverLicenseRepository;
        }

        public async Task<Guid> UploadUserPhotoAsync(Guid userId, IFormFile file)
        {
            await ValidateFileAsync(file, new[] { ".jpg", ".jpeg", ".png" }, 2 * 1024 * 1024); // 2MB max

            // Deactivate existing user photo
            var existingFile = await _fileStorageRepository.GetActiveFileByEntityAsync(userId, "UserAccount", "UserPhoto");
            if (existingFile != null)
            {
                await _fileStorageRepository.DeactivateFileAsync(existingFile.Id);
            }

            var fileStorage = new FileStorage
            {
                FileName = $"user_photo_{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}{Path.GetExtension(file.FileName)}",
                OriginalFileName = file.FileName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                FileContent = await GetFileBytesAsync(file),
                FileType = "UserPhoto",
                EntityId = userId,
                EntityType = "UserAccount",
                UploadedBy = userId,
                IsActive = true
            };

            var createdFile = await _fileStorageRepository.AddAsync(fileStorage);

            // Update UserAccount with file ID
            var userAccount = await _userAccountRepository.FindAsync(userId);
            if (userAccount != null)
            {
                userAccount.UserPhotoFileId = createdFile.Id;
                userAccount.UpdatedAt = DateTime.UtcNow;
                await _userAccountRepository.UpdateAsync(userAccount);
            }

            return createdFile.Id;
        }

        public async Task<Guid> UploadHealthCertificateAsync(Guid driverId, IFormFile file)
        {
            await ValidateFileAsync(file, new[] { ".pdf", ".jpg", ".jpeg", ".png" }, 5 * 1024 * 1024); // 5MB max

            // Deactivate existing health certificate
            var existingFile = await _fileStorageRepository.GetActiveFileByEntityAsync(driverId, "Driver", "HealthCertificate");
            if (existingFile != null)
            {
                await _fileStorageRepository.DeactivateFileAsync(existingFile.Id);
            }

            var fileStorage = new FileStorage
            {
                FileName = $"health_certificate_{driverId}_{DateTime.UtcNow:yyyyMMddHHmmss}{Path.GetExtension(file.FileName)}",
                OriginalFileName = file.FileName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                FileContent = await GetFileBytesAsync(file),
                FileType = "HealthCertificate",
                EntityId = driverId,
                EntityType = "Driver",
                UploadedBy = driverId,
                IsActive = true
            };

            var createdFile = await _fileStorageRepository.AddAsync(fileStorage);

            // Update Driver with file ID
            var driver = await _driverRepository.FindAsync(driverId);
            if (driver != null)
            {
                driver.HealthCertificateFileId = createdFile.Id;
                driver.UpdatedAt = DateTime.UtcNow;
                await _driverRepository.UpdateAsync(driver);
            }

            return createdFile.Id;
        }

        public async Task<Guid> UploadLicenseImageAsync(Guid driverLicenseId, IFormFile file)
        {
            await ValidateFileAsync(file, new[] { ".jpg", ".jpeg", ".png", ".pdf" }, 5 * 1024 * 1024); // 5MB max

            // Deactivate existing license image
            var existingFile = await _fileStorageRepository.GetActiveFileByEntityAsync(driverLicenseId, "DriverLicense", "LicenseImage");
            if (existingFile != null)
            {
                await _fileStorageRepository.DeactivateFileAsync(existingFile.Id);
            }

            var fileStorage = new FileStorage
            {
                FileName = $"license_image_{driverLicenseId}_{DateTime.UtcNow:yyyyMMddHHmmss}{Path.GetExtension(file.FileName)}",
                OriginalFileName = file.FileName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                FileContent = await GetFileBytesAsync(file),
                FileType = "LicenseImage",
                EntityId = driverLicenseId,
                EntityType = "DriverLicense",
                UploadedBy = driverLicenseId, // Assuming the driver license ID is the creator
                IsActive = true
            };

            var createdFile = await _fileStorageRepository.AddAsync(fileStorage);

            // Update DriverLicense with file ID
            var driverLicense = await _driverLicenseRepository.FindAsync(driverLicenseId);
            if (driverLicense != null)
            {
                driverLicense.LicenseImageFileId = createdFile.Id;
                driverLicense.UpdatedAt = DateTime.UtcNow;
                await _driverLicenseRepository.UpdateAsync(driverLicense);
            }

            return createdFile.Id;
        }

        public async Task<byte[]> GetFileAsync(Guid fileId)
        {
            var fileStorage = await _fileStorageRepository.FindAsync(fileId);
            if (fileStorage == null || !fileStorage.IsActive)
                throw new InvalidOperationException("File not found or inactive.");

            return fileStorage.FileContent;
        }

        public async Task<bool> DeleteFileAsync(Guid fileId)
        {
            return await _fileStorageRepository.DeactivateFileAsync(fileId);
        }

        public async Task<string> GetFileContentTypeAsync(Guid fileId)
        {
            var fileStorage = await _fileStorageRepository.FindAsync(fileId);
            if (fileStorage == null || !fileStorage.IsActive)
                throw new InvalidOperationException("File not found or inactive.");

            return fileStorage.ContentType;
        }

        private async Task ValidateFileAsync(IFormFile file, string[] allowedExtensions, long maxSize)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("File is empty.");

            if (file.Length > maxSize)
                throw new InvalidOperationException($"File size must not exceed {maxSize / (1024 * 1024)}MB.");

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
                throw new InvalidOperationException($"Invalid file type. Allowed types: {string.Join(", ", allowedExtensions)}");
        }

        private async Task<byte[]> GetFileBytesAsync(IFormFile file)
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }
    }
}
