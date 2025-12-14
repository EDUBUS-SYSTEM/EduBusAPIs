using AutoMapper;
using Constants;
using Data.Models;
using Data.Models.Enums;
using Services.Models.AcademicCalendar;
using Services.Models.Driver;
using Services.Models.DriverVehicle;
using Services.Models.Notification;
using Services.Models.Parent;
using Services.Models.Supervisor;
using Services.Models.Payment;
using Services.Models.Route;
using Services.Models.RouteSchedule;
using Services.Models.Schedule;
using Services.Models.School;
using Services.Models.Student;
using Services.Models.StudentAbsenceRequest;
using Services.Models.StudentGrade;
using Services.Models.Trip;
using Services.Models.TripIncident;
using Services.Models.UnitPrice;
using Services.Models.UserAccount;
using Services.Models.Vehicle;
using Services.Models.EnrollmentSemesterSettings;
using System.Linq;
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

            // supervisor mapping
            CreateMap<CreateSupervisorRequest, Supervisor>();
            CreateMap<ImportSupervisorDto, Supervisor>()
               .ForMember(dest => dest.DateOfBirth,
               opt => opt.MapFrom(src => DateHelper.ParseDate(src.DateOfBirthString)));
            CreateMap<Supervisor, CreateUserResponse>();
            CreateMap<Supervisor, ImportUserSuccess>();
            CreateMap<Supervisor, SupervisorResponse>();

            // supervisor vehicle mapping
            CreateMap<SupervisorVehicle, Services.Models.SupervisorVehicle.SupervisorAssignmentDto>()
                .ForMember(dest => dest.SupervisorName, opt => opt.MapFrom(src => src.Supervisor != null ? $"{src.Supervisor.FirstName} {src.Supervisor.LastName}".Trim() : string.Empty))
                .ForMember(dest => dest.SupervisorEmail, opt => opt.MapFrom(src => src.Supervisor != null ? src.Supervisor.Email : string.Empty))
                .ForMember(dest => dest.SupervisorPhone, opt => opt.MapFrom(src => src.Supervisor != null ? src.Supervisor.PhoneNumber : string.Empty))
                .ForMember(dest => dest.VehiclePlate, opt => opt.MapFrom(src => src.Vehicle != null ? SecurityHelper.DecryptFromBytes(src.Vehicle.HashedLicensePlate) : string.Empty))
                .ForMember(dest => dest.VehicleCapacity, opt => opt.MapFrom(src => src.Vehicle != null ? src.Vehicle.Capacity : 0))
                .ForMember(dest => dest.AssignedByAdminName, opt => opt.MapFrom(src => src.AssignedByAdmin != null ? $"{src.AssignedByAdmin.FirstName} {src.AssignedByAdmin.LastName}".Trim() : string.Empty))
                .ForMember(dest => dest.ApprovedByAdminName, opt => opt.MapFrom(src => src.ApprovedByAdmin != null ? $"{src.ApprovedByAdmin.FirstName} {src.ApprovedByAdmin.LastName}".Trim() : string.Empty))
                .AfterMap((src, dest) =>
                {
                    var now = DateTime.UtcNow;
                    dest.IsActive = src.StartTimeUtc <= now && (src.EndTimeUtc == null || src.EndTimeUtc > now);
                });

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
            CreateMap<Student, StudentDto>()
                .ForMember(dest => dest.StudentImageId, 
                    opt => opt.MapFrom(src => src.Images.Any() ? (Guid?)src.Images.First().Id : null));
            CreateMap<CreateStudentRequest, Student>();
            CreateMap<UpdateStudentRequest, Student>();
            CreateMap<ImportStudentDto, Student>();
            CreateMap<Student, ImportStudentSuccess>();

            //student grade mapping
            CreateMap<CreateStudentGradeRequest, StudentGradeEnrollment>();
            CreateMap<UpdateStudentGradeResponse, StudentGradeEnrollment>();
            CreateMap<StudentGradeEnrollment, StudentGradeDto>();

            // user account mapping
            CreateMap<UserAccount, UserDto>()
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => GetUserRole(src)));
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
                .ForMember(dest => dest.ApprovedByAdminName, opt => opt.MapFrom(src =>
                    src.ApprovedByAdmin != null ? $"{src.ApprovedByAdmin.FirstName} {src.ApprovedByAdmin.LastName}" : null));
            CreateMap<ApproveLeaveRequestDto, DriverLeaveRequest>();
            CreateMap<RejectLeaveRequestDto, DriverLeaveRequest>();
            CreateMap<UpdateLeaveRequestDto, DriverLeaveRequest>();

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

            //DriverVehicle
            CreateMap<Vehicle, Services.Models.DriverVehicle.VehicleInfoDto>()
                .ForMember(d => d.LicensePlate, opt => opt.MapFrom(s => SecurityHelper.DecryptFromBytes(s.HashedLicensePlate)))
                .ForMember(d => d.VehicleType, opt => opt.MapFrom(_ => "Bus"))
                .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.Description, opt => opt.MapFrom(s => s.StatusNote));
            CreateMap<UserAccount, Services.Models.DriverVehicle.AdminInfoDto>()
                .ForMember(d => d.FullName, opt => opt.MapFrom(s => $"{s.FirstName} {s.LastName}"));

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
            // Schedule mappings
            CreateMap<Schedule, ScheduleDto>();
            CreateMap<CreateScheduleDto, Schedule>();
            CreateMap<UpdateScheduleDto, Schedule>()
                .ForMember(dest => dest.TimeOverrides, opt => opt.Ignore());

            // Trip mappings
            CreateMap<Trip.VehicleLocation, TripLocationDto>();
            CreateMap<TripLocationDto, Trip.VehicleLocation>();
            CreateMap<Trip, TripDto>()
                .ForMember(dest => dest.Stops, opt => opt.Ignore()); // Stops are mapped manually in controller
            CreateMap<CreateTripDto, Trip>()
                .ForMember(dest => dest.Stops, opt => opt.Ignore()); // Stops will be generated from route in service
            CreateMap<UpdateTripDto, Trip>()
                .ForMember(dest => dest.Stops, opt => opt.Ignore()); // Stops will be handled separately in service
            CreateMap<ScheduleSnapshot, ScheduleSnapshotDto>();
            CreateMap<ScheduleSnapshotDto, ScheduleSnapshot>();
            CreateMap<Trip.VehicleSnapshot, VehicleSnapshotDto>();
            CreateMap<VehicleSnapshotDto, Trip.VehicleSnapshot>();
            CreateMap<Trip.DriverSnapshot, DriverSnapshotDto>();
            CreateMap<DriverSnapshotDto, Trip.DriverSnapshot>();
            CreateMap<Trip.SupervisorSnapshot, SupervisorSnapshotDto>();
            CreateMap<SupervisorSnapshotDto, Trip.SupervisorSnapshot>();
            CreateMap<TripStopDto, TripStop>();

            // Driver Schedule mappings
            CreateMap<Trip, DriverScheduleDto>()
                .ForMember(dest => dest.RouteName, opt => opt.MapFrom(src => "Route Name")) // Will be populated by service
                .ForMember(dest => dest.ScheduleName, opt => opt.MapFrom(src => src.ScheduleSnapshot.Name))
                .ForMember(dest => dest.TotalStops, opt => opt.MapFrom(src => src.Stops.Count))
                .ForMember(dest => dest.CompletedStops, opt => opt.MapFrom(src => src.Stops.Count(s => s.DepartedAt.HasValue)))
                .ForMember(dest => dest.Stops, opt => opt.MapFrom(src => src.Stops));

            CreateMap<TripStop, DriverScheduleStopDto>()
                .ForMember(dest => dest.PickupPointName, opt => opt.MapFrom(src => "Pickup Point")) // Will be populated by service
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Location.Address))
                .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Location.Latitude))
                .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Location.Longitude))
                .ForMember(dest => dest.TotalStudents, opt => opt.MapFrom(src => src.Attendance.Count))
                .ForMember(dest => dest.PresentStudents, opt => opt.MapFrom(src => src.Attendance.Count(a => a.State == "Present")))
                .ForMember(dest => dest.AbsentStudents, opt => opt.MapFrom(src => src.Attendance.Count(a => a.State == "Absent")));

            CreateMap<DriverScheduleSummary, DriverScheduleSummaryDto>();

            // RouteSchedule mappings
            CreateMap<RouteSchedule, RouteScheduleDto>();
            CreateMap<CreateRouteScheduleDto, RouteSchedule>();
            CreateMap<UpdateRouteScheduleDto, RouteSchedule>();

            // AcademicCalendar mappings
            CreateMap<AcademicCalendar, AcademicCalendarDto>();
            CreateMap<AcademicCalendarCreateDto, AcademicCalendar>();
            CreateMap<AcademicCalendarUpdateDto, AcademicCalendar>();
            CreateMap<AcademicSemester, AcademicSemesterDto>();
            CreateMap<AcademicSemesterDto, AcademicSemester>();
            CreateMap<SchoolHoliday, SchoolHolidayDto>();
            CreateMap<SchoolHolidayDto, SchoolHoliday>();
            CreateMap<SchoolDay, SchoolDayDto>();
            CreateMap<SchoolDayDto, SchoolDay>();

            // UnitPrice mapping
            CreateMap<UnitPrice, UnitPriceResponseDto>();
            CreateMap<CreateUnitPriceDto, UnitPrice>();
            CreateMap<UpdateUnitPriceDto, UnitPrice>();

            // School mapping
            // Base entity to DTO mapping (file content mapped separately via extensions)
            CreateMap<School, SchoolDto>()
                .ForMember(dest => dest.LogoFileId, opt => opt.Ignore())
                .ForMember(dest => dest.LogoImageBase64, opt => opt.Ignore())
                .ForMember(dest => dest.LogoImageContentType, opt => opt.Ignore())
                .ForMember(dest => dest.BannerImage, opt => opt.Ignore())
                .ForMember(dest => dest.StayConnectedImage, opt => opt.Ignore())
                .ForMember(dest => dest.FeatureImage, opt => opt.Ignore())
                .ForMember(dest => dest.GalleryImages, opt => opt.Ignore());

            // Request to entity mappings
            CreateMap<CreateSchoolRequest, School>();
            CreateMap<UpdateSchoolRequest, School>();

            // StudentAbsenceRequest request mapping
            CreateMap<CreateStudentAbsenceRequestDto, StudentAbsenceRequest>()
            .ForMember(d => d.Status, o => o.MapFrom(_ => AbsenceRequestStatus.Pending))
            .ForMember(d => d.CreatedAt, o => o.MapFrom(_ => DateTime.UtcNow));

            CreateMap<UpdateStudentAbsenceStatusDto, StudentAbsenceRequest>()
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.ReviewedAt, o => o.MapFrom(_ => DateTime.UtcNow))
                .ForMember(d => d.UpdatedAt, o => o.MapFrom(_ => DateTime.UtcNow));

            CreateMap<StudentAbsenceRequest, StudentAbsenceRequestResponseDto>();
            CreateMap<StudentAbsenceRequest, StudentAbsenceRequestListItemDto>();

            // Trip incident mappings
            CreateMap<TripIncidentReport, TripIncidentResponseDto>();
            CreateMap<TripIncidentReport, TripIncidentListItemDto>();

            // EnrollmentSemesterSettings mappings
            CreateMap<EnrollmentSemesterSettings, EnrollmentSemesterSettingsDto>();
            CreateMap<EnrollmentSemesterSettingsCreateDto, EnrollmentSemesterSettings>();
            CreateMap<EnrollmentSemesterSettingsUpdateDto, EnrollmentSemesterSettings>();
        }
            private static string GetUserRole(UserAccount user)
            {
            return user switch
            {
                Admin => Constants.Roles.Admin,
                Driver => Constants.Roles.Driver,
                Parent => Constants.Roles.Parent,
                Supervisor => Constants.Roles.Supervisor,
                _ => Constants.Roles.Unknown
            };
            
    }
    }
}
