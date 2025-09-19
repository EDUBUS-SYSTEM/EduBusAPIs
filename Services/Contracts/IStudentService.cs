using Services.Models.Student;

namespace Services.Contracts
{
    public interface IStudentService
    {
        Task<StudentDto> CreateStudentAsync(CreateStudentRequest request);
        Task<StudentDto> UpdateStudentAsync(Guid id, UpdateStudentRequest request);
        Task<StudentDto?> GetStudentByIdAsync(Guid id);
        Task<IEnumerable<StudentDto>> GetAllStudentsAsync();
        Task<IEnumerable<StudentDto>> GetStudentsByParentAsync(Guid parentId);
        Task<ImportStudentResult> ImportStudentsFromExcelAsync(Stream excelFileStream);
        Task<byte[]> ExportStudentsToExcelAsync();
        
        // Status management methods
        Task<StudentDto> ActivateStudentAsync(Guid id);
        Task<StudentDto> DeactivateStudentAsync(Guid id, string reason);
        Task<StudentDto> RestoreStudentAsync(Guid id);
        Task<StudentDto> SoftDeleteStudentAsync(Guid id, string reason);
        Task<IEnumerable<StudentDto>> GetStudentsByStatusAsync(Data.Models.Enums.StudentStatus status);
        
        // TODO: Will be used when payment service is ready
        Task<StudentDto> ActivateStudentByPaymentAsync(Guid id);
    }
}
