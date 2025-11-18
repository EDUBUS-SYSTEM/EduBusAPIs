using Data.Models;
using Data.Models.Enums;
using Microsoft.AspNetCore.Http;
using Services.Models.StudentAbsenceRequest;

namespace Services.Contracts
{
    public interface IStudentAbsenceRequestService
    {
        Task<StudentAbsenceRequestResponseDto> CreateAsync(CreateStudentAbsenceRequestDto request, HttpContext httpContext);
        Task<StudentAbsenceRequestResponseDto?> GetByIdAsync(Guid requestId);
        Task<StudentAbsenceRequestListResponse> GetByStudentAsync(
            Guid studentId,
            DateTime? startDate,
            DateTime? endDate,
            AbsenceRequestStatus? status,
            CreateAtSortOption sort,
            int page,
            int perPage);

        Task<StudentAbsenceRequestListResponse> GetByParentAsync(
            Guid parentId,
            DateTime? startDate,
            DateTime? endDate,
            AbsenceRequestStatus? status,
            CreateAtSortOption sort,
            int page,
            int perPage);
        Task<StudentAbsenceRequestListResponse> GetAllAsync(
            DateTime? startDate,
            DateTime? endDate,
            AbsenceRequestStatus? status,
            string? studentName,
            CreateAtSortOption sort,
            int page,
            int perPage);
        Task<StudentAbsenceRequest?> GetPendingOverlapAsync(Guid studentId, DateTime startDate, DateTime endDate);
        Task<StudentAbsenceRequestResponseDto?> UpdateStatusAsync(UpdateStudentAbsenceStatusDto request);
        Task<StudentAbsenceRequestResponseDto> RejectRequestAsync(Guid requestId, RejectStudentAbsenceRequestDto dto, Guid adminId);
        Task<StudentAbsenceRequestResponseDto> ApproveRequestAsync(Guid requestId, ApproveStudentAbsenceRequestDto dto, Guid adminId);
    }
}