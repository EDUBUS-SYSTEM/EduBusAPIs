using Data.Models;
using Services.Models.StudentAbsenceRequest;

namespace Services.Contracts
{
    public interface IStudentAbsenceRequestService
    {
        Task<StudentAbsenceRequestResponseDto> CreateAsync(CreateStudentAbsenceRequestDto request);
        Task<IEnumerable<StudentAbsenceRequestResponseDto>> GetByStudentAsync(Guid studentId);
        Task<IEnumerable<StudentAbsenceRequestResponseDto>> GetByParentAsync(Guid parentId);
        Task<StudentAbsenceRequest?> GetPendingOverlapAsync(Guid studentId, DateTime startDate, DateTime endDate);
        Task<StudentAbsenceRequestResponseDto?> UpdateStatusAsync(UpdateStudentAbsenceStatusDto request);
    }
}