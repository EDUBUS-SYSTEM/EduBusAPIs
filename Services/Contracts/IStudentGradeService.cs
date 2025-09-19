using Services.Models.StudentGrade;

namespace Services.Contracts
{
    public interface IStudentGradeService
    {
        Task<StudentGradeDto> CreateStudentGradeAsync(CreateStudentGradeRequest request);
        Task<StudentGradeDto> UpdateStudentGradeAsync(UpdateStudentGradeResponse request);
        Task<IEnumerable<StudentGradeDto>> GetAllStudentGradesAsync();
    }
}
