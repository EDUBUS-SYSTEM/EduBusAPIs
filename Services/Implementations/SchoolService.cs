using Data.Contexts.SqlServer;
using Data.Models;
using Data.Repos.Interfaces;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Services.Contracts;
using Services.Models.School;
using System.Collections.Generic;
using System.Linq;

namespace Services.Implementations;

public class SchoolService : ISchoolService
{
    private readonly ISqlRepository<School> _schoolRepository;
    private readonly EduBusSqlContext _dbContext;
    private readonly IFileStorageRepository _fileStorageRepository;

    public SchoolService(
        ISqlRepository<School> schoolRepository, 
        EduBusSqlContext dbContext,
        IFileStorageRepository fileStorageRepository)
    {
        _schoolRepository = schoolRepository;
        _dbContext = dbContext;
        _fileStorageRepository = fileStorageRepository;
    }

    public async Task<SchoolDto?> GetSchoolAsync()
    {
        var school = await _schoolRepository
            .FindByConditionAsync(s => s.IsActive && !s.IsDeleted)
            .ContinueWith(t => t.Result.FirstOrDefault());

        if (school == null)
            return null;

        return await MapToDtoAsync(school);
    }

    public async Task<SchoolDto?> GetSchoolForAdminAsync()
    {
        var school = await _schoolRepository
            .FindByConditionAsync(s => !s.IsDeleted)
            .ContinueWith(t => t.Result.FirstOrDefault());

        if (school == null)
            return null;

        return await MapToDtoAsync(school);
    }

    public async Task<SchoolDto> CreateSchoolAsync(CreateSchoolRequest request)
    {
        var existingSchools = await _schoolRepository.FindByConditionAsync(s => !s.IsDeleted);
        if (existingSchools.Any())
        {
            throw new InvalidOperationException("School already exists. Please update the existing record.");
        }

        var school = new School
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        ApplySchoolData(school, request);
        school.UpdatedAt = DateTime.UtcNow;

        var createdSchool = await _schoolRepository.AddAsync(school);
        return await MapToDtoAsync(createdSchool);
    }

    public async Task<SchoolDto> UpdateSchoolAsync(Guid id, UpdateSchoolRequest request)
    {
        var school = await _schoolRepository.FindAsync(id);
        if (school == null || school.IsDeleted)
        {
            throw new KeyNotFoundException("School not found.");
        }

        ApplySchoolData(school, request);
        school.UpdatedAt = DateTime.UtcNow;

        var updatedSchool = await _schoolRepository.UpdateAsync(school) ?? school;
        return await MapToDtoAsync(updatedSchool);
    }

    public async Task<SchoolDto> UpdateSchoolLocationAsync(Guid id, SchoolLocationRequest request)
    {
        var school = await _schoolRepository.FindAsync(id);
        if (school == null || school.IsDeleted)
        {
            throw new KeyNotFoundException("School not found.");
        }

        UpdateLocation(school, request.Latitude, request.Longitude);
        school.FullAddress = request.FullAddress;
        school.DisplayAddress = request.DisplayAddress;
        school.UpdatedAt = DateTime.UtcNow;

        var updatedSchool = await _schoolRepository.UpdateAsync(school) ?? school;
        return await MapToDtoAsync(updatedSchool);
    }

    private void UpdateLocation(School school, double latitude, double longitude)
    {
        var geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        school.Geog = geometryFactory.CreatePoint(new Coordinate(longitude, latitude)); 
        school.Latitude = latitude;
        school.Longitude = longitude;
    }

    private void ApplySchoolData(School school, SchoolWriteRequestBase request)
    {
        school.SchoolName = request.SchoolName;
        school.Slogan = request.Slogan;
        school.ShortDescription = request.ShortDescription;
        school.FullDescription = request.FullDescription;
        school.Email = request.Email;
        school.PhoneNumber = request.PhoneNumber;
        school.Website = request.Website;
        school.FullAddress = request.FullAddress;
        school.DisplayAddress = request.DisplayAddress;
        school.FooterText = request.FooterText;
        school.IsPublished = request.IsPublished;
        school.InternalNotes = request.InternalNotes;

        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            UpdateLocation(school, request.Latitude.Value, request.Longitude.Value);
        }
    }

    private async Task<SchoolDto> MapToDtoAsync(School school)
    {
        var logoFile = await _fileStorageRepository.GetActiveFileByEntityAsync(school.Id, "School", "Logo");
        var bannerFile = await _fileStorageRepository.GetActiveFileByEntityAsync(school.Id, "School", "Banner");
        var stayConnectedFile = await _fileStorageRepository.GetActiveFileByEntityAsync(school.Id, "School", "StayConnected");
        var featureFile = await _fileStorageRepository.GetActiveFileByEntityAsync(school.Id, "School", "FeatureHighlight");
        var allFiles = await _fileStorageRepository.GetFilesByEntityAsync(school.Id, "School");

        string? logoBase64 = null;

        if (logoFile?.FileContent is { Length: > 0 })
        {
            logoBase64 = Convert.ToBase64String(logoFile.FileContent);
        }

        var galleryImages = allFiles
            .Where(f => f.IsActive && f.FileType == "Gallery")
            .OrderByDescending(f => f.CreatedAt)
            .Select(MapToImageContentDto)
            .Where(dto => dto != null)
            .Cast<SchoolImageContentDto>()
            .ToList();

        return new SchoolDto
        {
            Id = school.Id,
            SchoolName = school.SchoolName,
            Slogan = school.Slogan,
            ShortDescription = school.ShortDescription,
            FullDescription = school.FullDescription,
            Email = school.Email,
            PhoneNumber = school.PhoneNumber,
            Website = school.Website,
            FullAddress = school.FullAddress,
            DisplayAddress = school.DisplayAddress,
            Latitude = school.Latitude,
            Longitude = school.Longitude,
            FooterText = school.FooterText,
            LogoFileId = logoFile?.Id,
            LogoImageBase64 = logoBase64,
            LogoImageContentType = logoFile?.ContentType,
            BannerImage = MapToImageContentDto(bannerFile),
            StayConnectedImage = MapToImageContentDto(stayConnectedFile),
            FeatureImage = MapToImageContentDto(featureFile),
            GalleryImages = galleryImages,
            IsPublished = school.IsPublished,
            IsActive = school.IsActive,
            InternalNotes = school.InternalNotes,
            CreatedAt = school.CreatedAt,
            UpdatedAt = school.UpdatedAt
        };
    }

    public async Task<List<SchoolImageDto>> GetSchoolImagesAsync()
    {
        var schools = await _schoolRepository.FindByConditionAsync(s => !s.IsDeleted);
        var school = schools.FirstOrDefault();
        
        if (school == null)
            return new List<SchoolImageDto>();

        var files = await _fileStorageRepository.GetFilesByEntityAsync(school.Id, "School");
        var allowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Logo",
            "Banner",
            "StayConnected",
            "FeatureHighlight",
            "Gallery"
        };
        
        return files
            .Where(f => f.IsActive && allowedTypes.Contains(f.FileType))
            .Select(f => new SchoolImageDto
            {
                FileId = f.Id,
                FileType = f.FileType,
                OriginalFileName = f.OriginalFileName,
                ContentType = f.ContentType,
                UploadedAt = f.CreatedAt
            })
            .ToList();
    }

    private static SchoolImageContentDto? MapToImageContentDto(Data.Models.FileStorage? file)
    {
        if (file?.FileContent == null || file.FileContent.Length == 0)
            return null;

        return new SchoolImageContentDto
        {
            FileId = file.Id,
            FileType = file.FileType,
            ContentType = file.ContentType,
            Base64Data = Convert.ToBase64String(file.FileContent),
            UploadedAt = file.CreatedAt
        };
    }
}

