using Services.Models.Student;
using Services.Models.UserAccount;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        // New admin workflow methods
        Task<IEnumerable<StudentDto>> GetUnassignedStudentsAsync();
        Task<BulkAssignParentResponse> BulkAssignParentAsync(BulkAssignParentRequest request);
    }
}
