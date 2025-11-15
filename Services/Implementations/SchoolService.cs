using AutoMapper;
using Data.Contexts.SqlServer;
using Data.Models;
using Data.Repos.Interfaces;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Services.Contracts;
using Services.Extensions;
using Services.Models.School;

namespace Services.Implementations;

public class SchoolService : ISchoolService
{
    private readonly ISqlRepository<School> _schoolRepository;
    private readonly EduBusSqlContext _dbContext;
    private readonly IFileStorageRepository _fileStorageRepository;
    private readonly IMapper _mapper;

    public SchoolService(
        ISqlRepository<School> schoolRepository,
        EduBusSqlContext dbContext,
        IFileStorageRepository fileStorageRepository,
        IMapper mapper)
    {
        _schoolRepository = schoolRepository;
        _dbContext = dbContext;
        _fileStorageRepository = fileStorageRepository;
        _mapper = mapper;
    }

    public async Task<SchoolDto?> GetSchoolAsync()
    {
        var school = await _schoolRepository
            .FindByConditionAsync(s => s.IsActive && !s.IsDeleted)
            .ContinueWith(t => t.Result.FirstOrDefault());

        if (school == null)
            return null;

        return await school.ToSchoolDtoAsync(_mapper, _fileStorageRepository);
    }

    public async Task<SchoolDto?> GetSchoolForAdminAsync()
    {
        var school = await _schoolRepository
            .FindByConditionAsync(s => !s.IsDeleted)
            .ContinueWith(t => t.Result.FirstOrDefault());

        if (school == null)
            return null;

        return await school.ToSchoolDtoAsync(_mapper, _fileStorageRepository);
    }

    public async Task<SchoolDto> CreateSchoolAsync(CreateSchoolRequest request)
    {
        var existingSchools = await _schoolRepository.FindByConditionAsync(s => !s.IsDeleted);
        if (existingSchools.Any())
        {
            throw new InvalidOperationException("School already exists. Please update the existing record.");
        }

        var school = _mapper.Map<School>(request);
        school.Id = Guid.NewGuid();
        school.CreatedAt = DateTime.UtcNow;
        school.UpdatedAt = DateTime.UtcNow;
        school.IsActive = true;

        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            UpdateLocation(school, request.Latitude.Value, request.Longitude.Value);
        }

        var createdSchool = await _schoolRepository.AddAsync(school);

        return await createdSchool.ToSchoolDtoAsync(_mapper, _fileStorageRepository);
    }

    public async Task<SchoolDto> UpdateSchoolAsync(Guid id, UpdateSchoolRequest request)
    {
        var school = await _schoolRepository.FindAsync(id);
        if (school == null || school.IsDeleted)
        {
            throw new KeyNotFoundException("School not found.");
        }

        _mapper.Map(request, school);
        school.UpdatedAt = DateTime.UtcNow;

        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            UpdateLocation(school, request.Latitude.Value, request.Longitude.Value);
        }

        var updatedSchool = await _schoolRepository.UpdateAsync(school) ?? school;

        return await updatedSchool.ToSchoolDtoAsync(_mapper, _fileStorageRepository);
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

        return await updatedSchool.ToSchoolDtoAsync(_mapper, _fileStorageRepository);
    }

    private void UpdateLocation(School school, double latitude, double longitude)
    {
        var geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        school.Geog = geometryFactory.CreatePoint(new Coordinate(longitude, latitude));
        school.Latitude = latitude;
        school.Longitude = longitude;
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
}

