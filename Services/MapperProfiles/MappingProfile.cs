using AutoMapper;
using Data.Models;
using Services.Models.Driver;
using Services.Models.Parent;
using Services.Models.UserAccount;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        }
    }
}
