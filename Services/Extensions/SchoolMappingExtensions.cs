using AutoMapper;
using Data.Models;
using Data.Repos.Interfaces;
using Services.Models.School;

namespace Services.Extensions;

/// <summary>
/// Extension methods for School entity mapping with file storage
/// </summary>
public static class SchoolMappingExtensions
{
    /// <summary>
    /// Maps School entity to SchoolDto with file content loaded from MongoDB
    /// </summary>
    public static async Task<SchoolDto> ToSchoolDtoAsync(
        this School school,
        IMapper mapper,
        IFileStorageRepository fileStorageRepository)
    {
        // Use AutoMapper for basic property mapping
        var dto = mapper.Map<SchoolDto>(school);

        // Load and map file storage data
        await MapFileStorageAsync(dto, school.Id, fileStorageRepository);

        return dto;
    }

    /// <summary>
    /// Maps collection of School entities to SchoolDto collection
    /// </summary>
    public static async Task<List<SchoolDto>> ToSchoolDtosAsync(
        this IEnumerable<School> schools,
        IMapper mapper,
        IFileStorageRepository fileStorageRepository)
    {
        var dtos = new List<SchoolDto>();
        foreach (var school in schools)
        {
            dtos.Add(await school.ToSchoolDtoAsync(mapper, fileStorageRepository));
        }
        return dtos;
    }

    /// <summary>
    /// Loads file storage data and maps to DTO properties
    /// </summary>
    private static async Task MapFileStorageAsync(
        SchoolDto dto,
        Guid schoolId,
        IFileStorageRepository fileStorageRepository)
    {
        // Load files from MongoDB
        var logoFile = await fileStorageRepository.GetActiveFileByEntityAsync(schoolId, "School", "Logo");
        var bannerFile = await fileStorageRepository.GetActiveFileByEntityAsync(schoolId, "School", "Banner");
        var stayConnectedFile = await fileStorageRepository.GetActiveFileByEntityAsync(schoolId, "School", "StayConnected");
        var featureFile = await fileStorageRepository.GetActiveFileByEntityAsync(schoolId, "School", "FeatureHighlight");
        var allFiles = await fileStorageRepository.GetFilesByEntityAsync(schoolId, "School");

        // Map Logo (special handling for backward compatibility)
        if (logoFile?.FileContent is { Length: > 0 })
        {
            dto.LogoFileId = logoFile.Id;
            dto.LogoImageBase64 = Convert.ToBase64String(logoFile.FileContent);
            dto.LogoImageContentType = logoFile.ContentType;
        }

        // Map other images
        dto.BannerImage = MapToImageContentDto(bannerFile);
        dto.StayConnectedImage = MapToImageContentDto(stayConnectedFile);
        dto.FeatureImage = MapToImageContentDto(featureFile);

        // Map Gallery images
        dto.GalleryImages = allFiles
            .Where(f => f.IsActive && f.FileType == "Gallery")
            .OrderByDescending(f => f.CreatedAt)
            .Select(MapToImageContentDto)
            .Where(imageDto => imageDto != null)
            .Cast<SchoolImageContentDto>()
            .ToList();
    }

    /// <summary>
    /// Maps FileStorage to SchoolImageContentDto
    /// </summary>
    private static SchoolImageContentDto? MapToImageContentDto(FileStorage? file)
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
