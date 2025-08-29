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
        Task<StudentDto> UpdateStudentAsync(UpdateStudentRequest request);
        Task<StudentDto?> GetStudentByIdAsync(Guid id);
        Task<IEnumerable<StudentDto>> GetAllStudentsAsync();
        Task<IEnumerable<StudentDto>> GetStudentsByParentAsync(Guid parentId);
        Task<ImportStudentResult> ImportStudentsFromExcelAsync(Stream excelFileStream);
        Task<byte[]> ExportStudentsToExcelAsync();
    }
}
