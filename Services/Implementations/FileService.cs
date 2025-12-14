using Data.Models;
using Data.Repos.Interfaces;
using Microsoft.AspNetCore.Http;
using Services.Contracts;
using Utils;

namespace Services.Implementations
{
    public class FileService : IFileService
    {
        private readonly IFileStorageRepository _fileStorageRepository;
        private readonly IDriverRepository _driverRepository;
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IDriverLicenseRepository _driverLicenseRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public FileService(
            IFileStorageRepository fileStorageRepository,
            IDriverRepository driverRepository,
            IUserAccountRepository userAccountRepository,
            IDriverLicenseRepository driverLicenseRepository,
            IStudentRepository studentRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _fileStorageRepository = fileStorageRepository;
            _driverRepository = driverRepository;
            _userAccountRepository = userAccountRepository;
            _driverLicenseRepository = driverLicenseRepository;
            _studentRepository = studentRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Guid> UploadFileAsync(Guid entityId, string entityType, string fileType, IFormFile file)
        {
            // Validate file based on file type
            var (allowedExtensions, maxSize) = GetFileTypeValidation(fileType);
            await ValidateFileAsync(file, allowedExtensions, maxSize);

            // For template files, use Guid.Empty as entityId
            var actualEntityId = entityType.ToLower() == "template" ? Guid.Empty : entityId;

            // Deactivate existing file of the same type for this entity
            var existingFile = await _fileStorageRepository.GetActiveFileByEntityAsync(actualEntityId, entityType, fileType);
            if (existingFile != null)
            {
                await _fileStorageRepository.DeactivateFileAsync(existingFile.Id);
            }

            var httpContext = _httpContextAccessor.HttpContext;
            var currentUserId = httpContext != null ? AuthorizationHelper.GetCurrentUserId(httpContext) : null;
            var uploadedBy = currentUserId ?? actualEntityId;

            var fileStorage = new FileStorage
            {
                FileName = $"{fileType.ToLower()}_{actualEntityId}_{DateTime.UtcNow:yyyyMMddHHmmss}{Path.GetExtension(file.FileName)}",
                OriginalFileName = file.FileName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                FileContent = await GetFileBytesAsync(file),
                FileType = fileType,
                EntityId = actualEntityId,
                EntityType = entityType,
                UploadedBy = uploadedBy,
                IsActive = true
            };

            var createdFile = await _fileStorageRepository.AddAsync(fileStorage);

            // Update related entity if needed (skip for template files)
            if (entityType.ToLower() != "template")
            {
                await UpdateEntityWithFileId(entityId, entityType, fileType, createdFile.Id);
            }

            return createdFile.Id;
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

        public async Task<Guid?> GetUserPhotoFileIdAsync(Guid userId)
        {
            var userAccount = await _userAccountRepository.FindAsync(userId);
            return userAccount?.UserPhotoFileId;
        }

        public async Task<FileStorage?> GetTemplateFileAsync(string templateType)
        {
            if (string.IsNullOrWhiteSpace(templateType))
            {
                return null;
            }

            var normalized = templateType.Trim().ToLowerInvariant();
            var resolvedFileType = normalized switch
            {
                "useraccount" => "UserAccount",
                "driver" => "Driver",
                "parent" => "Parent",
                _ => templateType
            };

            var templateFile = await _fileStorageRepository.GetActiveFileByEntityAsync(Guid.Empty, "Template", resolvedFileType);
            return templateFile;
        }

        private (string[] allowedExtensions, long maxSize) GetFileTypeValidation(string fileType)
        {
            return fileType.ToLower() switch
            {
                "userphoto" => (new[] { ".jpg", ".jpeg", ".png" }, 2 * 1024 * 1024), // 2MB
                "studentphoto" => (new[] { ".jpg", ".jpeg", ".png" }, 2 * 1024 * 1024), // 2MB
                "healthcertificate" => (new[] { ".pdf", ".jpg", ".jpeg", ".png" }, 5 * 1024 * 1024), // 5MB
                "licenseimage" => (new[] { ".jpg", ".jpeg", ".png", ".pdf" }, 5 * 1024 * 1024), // 5MB
                "document" => (new[] { ".pdf", ".doc", ".docx", ".txt" }, 10 * 1024 * 1024), // 10MB
                "image" => (new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" }, 5 * 1024 * 1024), // 5MB
                "useraccount" => (new[] { ".xlsx" }, 10 * 1024 * 1024), // 10MB - Excel template
                "driver" => (new[] { ".xlsx" }, 10 * 1024 * 1024), // 10MB - Excel template
                "parent" => (new[] { ".xlsx" }, 10 * 1024 * 1024), // 10MB - Excel template
                _ => (new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx", ".txt" }, 10 * 1024 * 1024) // Default: 10MB
            };
        }

        private async Task UpdateEntityWithFileId(Guid entityId, string entityType, string fileType, Guid fileId)
        {
            switch (entityType.ToLower())
            {
                case "useraccount" when fileType.ToLower() == "userphoto":
                    var userAccount = await _userAccountRepository.FindAsync(entityId);
                    if (userAccount != null)
                    {
                        userAccount.UserPhotoFileId = fileId;
                        userAccount.UpdatedAt = DateTime.UtcNow;
                        await _userAccountRepository.UpdateAsync(userAccount);
                    }
                    break;

                case "driver" when fileType.ToLower() == "healthcertificate":
                    var driver = await _driverRepository.FindAsync(entityId);
                    if (driver != null)
                    {
                        driver.HealthCertificateFileId = fileId;
                        driver.UpdatedAt = DateTime.UtcNow;
                        await _driverRepository.UpdateAsync(driver);
                    }
                    break;

                case "driverlicense" when fileType.ToLower() == "licenseimage":
                    var driverLicense = await _driverLicenseRepository.FindAsync(entityId);
                    if (driverLicense != null)
                    {
                        driverLicense.LicenseImageFileId = fileId;
                        driverLicense.UpdatedAt = DateTime.UtcNow;
                        await _driverLicenseRepository.UpdateAsync(driverLicense);
                    }
                    break;

                case "student" when fileType.ToLower() == "studentphoto":
                    var student = await _studentRepository.FindAsync(entityId);
                    if (student != null)
                    {
                        student.StudentImageId = fileId;
                        student.UpdatedAt = DateTime.UtcNow;
                        await _studentRepository.UpdateAsync(student);
                    }
                    break;
            }
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
