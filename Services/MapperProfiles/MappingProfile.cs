using AutoMapper;
using Data.Models;
using Services.Models.Driver;
using Services.Models.DriverVehicle;
using Services.Models.Parent;
using Services.Models.UserAccount;
using Services.Models.Vehicle;
using Utils;

namespace Services.MapperProfiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // parent mapping
            CreateMap<CreateParentRequest, Parent>();
            CreateMap<ImportParentDto, Parent>()
               .ForMember(dest => dest.DateOfBirth,
               opt => opt.MapFrom(src => DateHelper.ParseDate(src.DateOfBirthString)));
            CreateMap<Parent, CreateUserResponse>();
            CreateMap<Parent, ImportUserSuccess>();

            // driver mapping
            CreateMap<CreateDriverRequest, Driver>();
            CreateMap<Driver, CreateUserResponse>();
            CreateMap<ImportDriverDto, Driver>()
               .ForMember(dest => dest.DateOfBirth,
               opt => opt.MapFrom(src => DateHelper.ParseDate(src.DateOfBirthString)));
            CreateMap<Driver, ImportUserSuccess>();

            // driver license mapping
            CreateMap<CreateDriverLicenseRequest, DriverLicense>();
            CreateMap<DriverLicense, DriverLicenseResponse>();

            // user account mapping
            CreateMap<UserAccount, UserDto>();
            CreateMap<UserAccount, UserResponse>();
            CreateMap<UserUpdateRequest, UserAccount>();

            //vehicle mapping
            CreateMap<Vehicle, VehicleDto>();
            CreateMap<VehicleCreateRequest, Vehicle>();
            CreateMap<VehicleUpdateRequest, Vehicle>();

            //DriverVehicle mapping
            CreateMap<DriverVehicle, DriverAssignmentDto>();
            CreateMap<Driver, DriverInfoDto>();
        }
    }
}
