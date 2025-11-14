using Services.Models.School;

namespace Services.Contracts;

public interface ISchoolService
{
  
    Task<SchoolDto?> GetSchoolAsync();

 
    Task<SchoolDto?> GetSchoolForAdminAsync();

    Task<SchoolDto> CreateSchoolAsync(CreateSchoolRequest request);

    Task<SchoolDto> UpdateSchoolAsync(Guid id, UpdateSchoolRequest request);
    Task<SchoolDto> UpdateSchoolLocationAsync(Guid id, SchoolLocationRequest request);
    Task<List<SchoolImageDto>> GetSchoolImagesAsync();
}

