using AutoMapper;
using Data.Models;
using Data.Repos.Interfaces;
using Services.Contracts;
using Services.Models.Student;
using Services.Models.StudentGrade;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class StudentGradeService : IStudentGradeService
    {
        private readonly IStudentGradeRepository _studentGradeRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IGradeRepository _gradeRepository;
        private readonly IMapper _mapper;
        public StudentGradeService(IStudentGradeRepository studentGradeRepository, 
            IStudentRepository studentRepository,
            IGradeRepository gradeRepository,
            IMapper mapper)
        {
            _studentGradeRepository = studentGradeRepository;
            _studentRepository = studentRepository;
            _gradeRepository = gradeRepository;
            _mapper = mapper;
        }
        public async Task<StudentGradeDto> CreateStudentGradeAsync(CreateStudentGradeRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
        
            if (request.EndTimeUtc <= request.StartTimeUtc)
                throw new ArgumentException("EndTimeUtc must be greater than StartTimeUtc.");
            var student = await _studentRepository.FindAsync(request.StudentId);
            if (student == null)
                throw new KeyNotFoundException("Student not found");
            var grade = await _gradeRepository.FindAsync(request.GradeId);
            if (grade == null)
                throw new KeyNotFoundException("Grade not found");

            var studentGrade = _mapper.Map<StudentGradeEnrollment>(request);
            var result = await _studentGradeRepository.AddAsync(studentGrade);
            return _mapper.Map<StudentGradeDto>(result);
        }

        public async Task<IEnumerable<StudentGradeDto>> GetAllStudentGradesAsync()
        {
            var studentGrades = await _studentGradeRepository.FindAllAsync() ?? new List<StudentGradeEnrollment>();
            return _mapper.Map<IEnumerable<StudentGradeDto>>(studentGrades);
        }

        public async Task<StudentGradeDto> UpdateStudentGradeAsync(UpdateStudentGradeResponse request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            var studentGrade = await _studentGradeRepository.FindAsync(request.Id);
            if (studentGrade == null)
                throw new KeyNotFoundException("Student grade not found.");
            if (request.EndTimeUtc <= request.StartTimeUtc)
                throw new ArgumentException("EndTimeUtc must be greater than StartTimeUtc.");
            var student = await _studentRepository.FindAsync(request.StudentId);
            if (student == null)
                throw new KeyNotFoundException("Student not found.");
            var grade = await _gradeRepository.FindAsync(request.GradeId);
            if (grade == null)
                throw new KeyNotFoundException("Grade not found.");

            var studentGradeUpdate = _mapper.Map<StudentGradeEnrollment>(request);
            var result = await _studentGradeRepository.UpdateAsync(studentGradeUpdate);
            return _mapper.Map<StudentGradeDto>(result);
        }
    }
}
