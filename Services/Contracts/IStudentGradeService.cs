using Services.Models.Student;
using Services.Models.StudentGrade;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Contracts
{
    public interface IStudentGradeService
    {
        Task<StudentGradeDto> CreateStudentGradeAsync(CreateStudentGradeRequest request);
        Task<StudentGradeDto> UpdateStudentGradeAsync(UpdateStudentGradeResponse request);
        Task<IEnumerable<StudentGradeDto>> GetAllStudentGradesAsync();
    }
}
