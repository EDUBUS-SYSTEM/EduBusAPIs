using AutoMapper;
using ClosedXML.Excel;
using Data.Models;
using Data.Repos.Interfaces;
using Data.Repos.SqlServer;
using Services.Contracts;
using Services.Models.Parent;
using Services.Models.Student;
using Services.Models.UserAccount;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace Services.Implementations
{
    public class StudentService : IStudentService
    {
        private readonly IStudentRepository _studentRepository;
        private readonly IParentRepository _parentRepository;
        private readonly IMapper _mapper;

        public StudentService(IStudentRepository studentRepository, IParentRepository parentRepository, IMapper mapper)
        {
            _studentRepository = studentRepository;
            _mapper = mapper;
            _parentRepository = parentRepository;
        }
        public async Task<StudentDto> CreateStudentAsync(CreateStudentRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            // If ParentId provided, ensure it exists; else allow null and keep ParentPhoneNumber for later linking
            if (request.ParentId.HasValue)
            {
                var parent = await _parentRepository.FindByConditionAsync(p => p.Id == request.ParentId.Value);
                if (!parent.Any())
                {
                    throw new KeyNotFoundException("Parent not found");
                }
            }

            var student = _mapper.Map<Student>(request);
            student.IsActive = true;

            await _studentRepository.AddAsync(student);

            return _mapper.Map<StudentDto>(student);
        }

        public async Task<StudentDto> UpdateStudentAsync(UpdateStudentRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            var student = await _studentRepository.FindAsync(request.Id);
            if (student == null)
                throw new KeyNotFoundException("Student not found");
            
            // If ParentId provided, ensure it exists
            if (request.ParentId.HasValue)
            {
                var parent = await _parentRepository.FindByConditionAsync(p => p.Id == request.ParentId.Value);
                if (!parent.Any())
                {
                    throw new KeyNotFoundException("Parent not found");
                }
                // Clear ParentPhoneNumber when linking to existing parent
                request.ParentPhoneNumber = string.Empty;
            }
            
            _mapper.Map(request, student);
            await _studentRepository.UpdateAsync(student);
            return _mapper.Map<StudentDto>(student);
        }

        public async Task<StudentDto?> GetStudentByIdAsync(Guid id)
        {
            var student = (await _studentRepository.FindByConditionAsync(st => st.Id == id)).FirstOrDefault();
            return student == null ? null : _mapper.Map<StudentDto>(student);
        }

        public async Task<IEnumerable<StudentDto>> GetAllStudentsAsync()
        {
            var students = await _studentRepository.FindAllAsync() ?? new List<Student>();
            return _mapper.Map<IEnumerable<StudentDto>>(students);
        }

        public async Task<IEnumerable<StudentDto>> GetStudentsByParentAsync(Guid parentId)
        {
            var students = await _studentRepository.FindByConditionAsync(st => st.ParentId == parentId) ?? new List<Student>();
            return _mapper.Map<IEnumerable<StudentDto>>(students);
        }

        public async Task<ImportStudentResult> ImportStudentsFromExcelAsync(Stream excelFileStream)
        {
            if (excelFileStream == null)
                throw new ArgumentNullException(nameof(excelFileStream));
            var result = new ImportStudentResult();
            var validStudentDtos = new List<(ImportStudentDto dto, int rowNumber)>();
            try
            {
                using var workbook = new XLWorkbook(excelFileStream);
                var worksheet = workbook.Worksheets.FirstOrDefault();

                if (worksheet == null)
                    throw new InvalidOperationException("Excel file does not contain any worksheets.");

                // Read data from Excel file (skip header row)
                var rows = worksheet.RangeUsed()?.RowsUsed().Skip(1);
                if (rows == null || !rows.Any())
                    throw new InvalidOperationException("Excel file does not contain any data rows.");
                // Parse data from Excel to DTO
                foreach (var row in rows)
                {
                    var rowNumber = row.RowNumber();
                    try
                    {
                        var studentDto = new ImportStudentDto()
                        {
                            FirstName = row.Cell(1).GetString().Trim(),
                            LastName = row.Cell(2).GetString().Trim(),
                            ParentPhoneNumber = row.Cell(3).GetString().Trim(),
                        };

                        // Validate basic data
                        if (string.IsNullOrWhiteSpace(studentDto.FirstName) ||
                            string.IsNullOrWhiteSpace(studentDto.LastName) ||
                            string.IsNullOrWhiteSpace(studentDto.ParentPhoneNumber))

                        {
                            result.FailedStudents.Add(new ImportStudentError
                            {
                                RowNumber = rowNumber,
                                FirstName = studentDto.FirstName,
                                LastName = studentDto.LastName,
                                ParentPhoneNumber = studentDto.ParentPhoneNumber,
                                ErrorMessage = "All properties are required."
                            });
                            continue;
                        }
                        validStudentDtos.Add((studentDto, rowNumber));
                    }
                    catch (Exception ex)
                    {
                        result.FailedStudents.Add(new ImportStudentError
                        {
                            RowNumber = rowNumber,
                            FirstName = "",
                            LastName = "",
                            ParentPhoneNumber = "",
                            ErrorMessage = $"Error parsing data: {ex.Message}"
                        });
                    }
                }
                
                // Set total number of records processed
                result.TotalProcessed = rows.Count();

                // test each student
                foreach (var (studentDto, rowNumber) in validStudentDtos)
                {
                    try
                    {
                        // Try to find existing parent by phone number
                        var parent = (await _parentRepository.FindByConditionAsync(p => p.PhoneNumber == studentDto.ParentPhoneNumber)).FirstOrDefault();
                        
                        // Map DTO to entity
                        var student = _mapper.Map<Student>(studentDto);
                        
                        if (parent != null)
                        {
                            // Link to existing parent and clear ParentPhoneNumber to avoid duplication
                            student.ParentId = parent.Id;
                            student.ParentPhoneNumber = string.Empty;
                        }
                        else
                        {
                            // Keep ParentPhoneNumber for later linking when parent registers
                            student.ParentId = null;
                            student.ParentPhoneNumber = studentDto.ParentPhoneNumber;
                        }
                        
                        student.IsActive = true;
                        // add database
                        var createdStudent = await _studentRepository.AddAsync(student);

                        var successResult = _mapper.Map<ImportStudentSuccess>(createdStudent);
                        successResult.RowNumber = rowNumber;
                        result.SuccessfulStudents.Add(successResult);
                    }
                    catch (Exception ex)
                    {
                        result.FailedStudents.Add(new ImportStudentError
                        {
                            RowNumber = rowNumber,
                            FirstName = studentDto.FirstName,
                            LastName = studentDto.LastName,
                            ParentPhoneNumber = studentDto.ParentPhoneNumber,
                            ErrorMessage = $"Error creating student: {ex.Message}"
                        });
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<byte[]> ExportStudentsToExcelAsync()
        {
            var students = await _studentRepository.FindAllAsync();
            if (students == null || !students.Any())
            {
                return Array.Empty<byte>();
            }
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Students");

                // Header
                worksheet.Cell(1, 1).Value = "First Name";
                worksheet.Cell(1, 2).Value = "Last Name";
                worksheet.Cell(1, 3).Value = "Parent Name";
                worksheet.Cell(1, 4).Value = "Parent Phone Number";
                worksheet.Cell(1, 5).Value = "Status";
                int row = 2;
                foreach (var st in students)
                {
                    worksheet.Cell(row, 1).Value = st.FirstName;
                    worksheet.Cell(row, 2).Value = st.LastName;
                    
                    if (st.ParentId.HasValue && st.Parent != null)
                    {
                        // Student is linked to parent
                        worksheet.Cell(row, 3).Value = $"{st.Parent.FirstName} {st.Parent.LastName}";
                        worksheet.Cell(row, 4).Value = st.Parent.PhoneNumber;
                        worksheet.Cell(row, 5).Value = "Linked";
                    }
                    else
                    {
                        // Student is not linked to parent yet
                        worksheet.Cell(row, 3).Value = "Not linked";
                        worksheet.Cell(row, 4).Value = st.ParentPhoneNumber;
                        worksheet.Cell(row, 5).Value = "Pending";
                    }
                    row++;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }
    }
}
