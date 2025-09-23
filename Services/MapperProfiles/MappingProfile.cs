using AutoMapper;
using Data.Models;
using Services.Models.Driver;
using Services.Models.DriverVehicle;
using Services.Models.Notification;
using Services.Models.Parent;
using Services.Models.Payment;
using Services.Models.Student;
using Services.Models.StudentGrade;
using Services.Models.UserAccount;
using Services.Models.Vehicle;
using Services.Models.Route;

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
            CreateMap<Driver, DriverResponse>();
            CreateMap<Driver, DriverStatusResponse>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));
            CreateMap<ImportDriverDto, Driver>()
               .ForMember(dest => dest.DateOfBirth,
               opt => opt.MapFrom(src => DateHelper.ParseDate(src.DateOfBirthString)));
            CreateMap<Driver, ImportUserSuccess>();

            // driver license mapping
            CreateMap<CreateDriverLicenseRequest, DriverLicense>();
            CreateMap<DriverLicense, DriverLicenseResponse>();

            // student mapping
            CreateMap<Student, StudentDto>();
            CreateMap<CreateStudentRequest, Student>();
            CreateMap<UpdateStudentRequest, Student>();
            CreateMap<ImportStudentDto, Student>();
            CreateMap<Student, ImportStudentSuccess>();
            
            //student grade mapping
            CreateMap<CreateStudentGradeRequest, StudentGradeEnrollment>();
            CreateMap<UpdateStudentGradeResponse, StudentGradeEnrollment>();
            CreateMap<StudentGradeEnrollment, StudentGradeDto>();

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
            
            // Driver Leave mapping
            CreateMap<CreateLeaveRequestDto, DriverLeaveRequest>();
            CreateMap<DriverLeaveRequest, DriverLeaveResponse>()
                .ForMember(dest => dest.DriverName, opt => opt.MapFrom(src => $"{src.Driver.FirstName} {src.Driver.LastName}"))
                .ForMember(dest => dest.DriverEmail, opt => opt.MapFrom(src => src.Driver.Email))
                .ForMember(dest => dest.TotalDays, opt => opt.MapFrom(src => (src.EndDate - src.StartDate).Days + 1));
            CreateMap<ApproveLeaveRequestDto, DriverLeaveRequest>();
            CreateMap<RejectLeaveRequestDto, DriverLeaveRequest>();
            CreateMap<UpdateLeaveRequestDto, DriverLeaveRequest>();
            
            // Driver Leave Conflict mapping
            CreateMap<DriverLeaveConflict, DriverLeaveConflictDto>();
            
            // Driver Working Hours mapping
            CreateMap<CreateWorkingHoursDto, DriverWorkingHours>();
            CreateMap<UpdateWorkingHoursDto, DriverWorkingHours>();
            CreateMap<DriverWorkingHours, DriverWorkingHoursResponse>()
                .ForMember(dest => dest.DriverName, opt => opt.MapFrom(src => $"{src.Driver.FirstName} {src.Driver.LastName}"));
            
            // Driver Vehicle Assignment mapping
            CreateMap<EnhancedDriverAssignmentRequest, DriverVehicle>();
            CreateMap<UpdateAssignmentRequest, DriverVehicle>();
            CreateMap<DriverVehicle, EnhancedDriverAssignmentResponse>()
                .ForMember(dest => dest.DriverName, opt => opt.MapFrom(src => $"{src.Driver.FirstName} {src.Driver.LastName}"))
                .ForMember(dest => dest.VehiclePlate, opt => opt.MapFrom(src => 
                    src.Vehicle != null ? SecurityHelper.DecryptFromBytes(src.Vehicle.HashedLicensePlate) : string.Empty))
                .ForMember(dest => dest.AssignmentId, opt => opt.MapFrom(src => src.Id));
            
            // Assignment Conflict mapping
            CreateMap<DriverVehicle, AssignmentConflictDto>();
            
            // Driver Assignment Summary mapping
            CreateMap<Driver, DriverAssignmentSummaryDto>()
                .ForMember(dest => dest.DriverName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
                .ForMember(dest => dest.DriverEmail, opt => opt.MapFrom(src => src.Email));
            
            // Notification mapping
            CreateMap<CreateNotificationDto, Notification>();
            CreateMap<Notification, NotificationResponse>();

            // Payment mapping
            CreateMap<Transaction, TransactionSummaryResponse>();
            CreateMap<Transaction, TransactionDetailResponse>();
            CreateMap<TransportFeeItem, TransportFeeItemResponse>();
            CreateMap<PaymentEventLog, PaymentEventResponse>();

            // Route mapping
            CreateMap<Route, RouteDto>();
            CreateMap<PickupPointInfo, PickupPointInfoDto>();
            CreateMap<LocationInfo, LocationInfoDto>();
        }
    }
}
