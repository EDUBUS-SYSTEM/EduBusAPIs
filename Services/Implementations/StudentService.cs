using AutoMapper;
using ClosedXML.Excel;
using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using Data.Repos.SqlServer;
using Microsoft.EntityFrameworkCore;
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

            // If ParentId is provided, ensure the parent exists
            Parent? linkedParent = null;
            if (request.ParentId.HasValue)
            {
                var parents = await _parentRepository.FindByConditionAsync(p => p.Id == request.ParentId.Value);
                linkedParent = parents.FirstOrDefault();
                if (linkedParent == null)
                    throw new KeyNotFoundException("Parent not found");
            }

            // Normalize and optionally validate email when not linking by ParentId
            var normalizedEmail = EmailHelper.NormalizeEmail(request.ParentEmail);

            if (!request.ParentId.HasValue)
            {
                if (string.IsNullOrEmpty(normalizedEmail))
                    throw new ArgumentException("Either ParentId or ParentEmail must be provided.");
                if (!EmailHelper.IsValidEmail(normalizedEmail))
                    throw new ArgumentException("Invalid ParentEmail.");
            }

            // Map request -> entity
            var student = _mapper.Map<Student>(request);
            student.Status = StudentStatus.Available;

            // Always keep ParentEmail on Student:
            // - If a ParentId is provided and request has a valid email, keep it.
            // - If a ParentId is provided but request email is missing/invalid, use parent's Email as a fallback.
            // - If no ParentId, we already validated and normalized above, so keep normalizedEmail.
            if (request.ParentId.HasValue)
            {
                if (!string.IsNullOrEmpty(normalizedEmail) && EmailHelper.IsValidEmail(normalizedEmail))
                {
                    student.ParentEmail = normalizedEmail;
                }
                else
                {
                    // fallback to parent's email if available
                    student.ParentEmail = EmailHelper.NormalizeEmail(linkedParent?.Email);
                }

                student.ParentId = request.ParentId;
            }
            else
            {
                student.ParentId = null;
                student.ParentEmail = normalizedEmail;
            }

            await _studentRepository.AddAsync(student);
            return _mapper.Map<StudentDto>(student);
        }

        public async Task<StudentDto> UpdateStudentAsync(Guid id, UpdateStudentRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var student = await _studentRepository.FindAsync(id);
            if (student == null)
                throw new KeyNotFoundException("Student not found");

            // If ParentId is provided, ensure the parent exists
            Parent? linkedParent = null;
            if (request.ParentId.HasValue)
            {
                var parents = await _parentRepository.FindByConditionAsync(p => p.Id == request.ParentId.Value);
                linkedParent = parents.FirstOrDefault();
                if (linkedParent == null)
                    throw new KeyNotFoundException("Parent not found");
            }

            // Normalize email coming from request (may be empty)
            var normalizedEmail = EmailHelper.NormalizeEmail(request.ParentEmail);

            // If not linking by ParentId and an email is provided, validate it
            if (!request.ParentId.HasValue && !string.IsNullOrEmpty(normalizedEmail))
            {
                if (!EmailHelper.IsValidEmail(normalizedEmail))
                    throw new ArgumentException("Invalid ParentEmail.");
            }

            // Map simple fields
            _mapper.Map(request, student);

            // Always keep ParentEmail on Student using the precedence:
            // 1) If request provides a valid email -> use it.
            // 2) Else if ParentId is provided -> fallback to parent's Email.
            // 3) Else -> clear if nothing valid is available.
            if (!string.IsNullOrEmpty(normalizedEmail) && EmailHelper.IsValidEmail(normalizedEmail))
            {
                student.ParentEmail = normalizedEmail;
            }
            else if (request.ParentId.HasValue)
            {
                student.ParentEmail = EmailHelper.NormalizeEmail(linkedParent?.Email);
            }
            else
            {
                student.ParentEmail = string.Empty;
            }

            // Maintain ParentId as requested
            student.ParentId = request.ParentId;

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

            using var workbook = new XLWorkbook(excelFileStream);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            if (worksheet == null)
                throw new InvalidOperationException("Excel file does not contain any worksheets.");

            var rows = worksheet.RangeUsed()?.RowsUsed().Skip(1);
            if (rows == null || !rows.Any())
                throw new InvalidOperationException("Excel file does not contain any data rows.");

            // Parse and validate rows
            foreach (var row in rows)
            {
                var rowNumber = row.RowNumber();
                try
                {
                    var studentDto = new ImportStudentDto
                    {
                        FirstName = row.Cell(1).GetString().Trim(),
                        LastName = row.Cell(2).GetString().Trim(),
                        ParentEmail = EmailHelper.NormalizeEmail(row.Cell(3).GetString())
                    };

                    if (string.IsNullOrWhiteSpace(studentDto.FirstName) ||
                        string.IsNullOrWhiteSpace(studentDto.LastName) ||
                        string.IsNullOrWhiteSpace(studentDto.ParentEmail))
                    {
                        result.FailedStudents.Add(new ImportStudentError
                        {
                            RowNumber = rowNumber,
                            FirstName = studentDto.FirstName,
                            LastName = studentDto.LastName,
                            ParentEmail = studentDto.ParentEmail,
                            ErrorMessage = "All properties are required."
                        });
                        continue;
                    }

                    if (!EmailHelper.IsValidEmail(studentDto.ParentEmail))
                    {
                        result.FailedStudents.Add(new ImportStudentError
                        {
                            RowNumber = rowNumber,
                            FirstName = studentDto.FirstName,
                            LastName = studentDto.LastName,
                            ParentEmail = studentDto.ParentEmail,
                            ErrorMessage = "Invalid email format."
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
                        ParentEmail = "",
                        ErrorMessage = $"Error parsing data: {ex.Message}"
                    });
                }
            }

            result.TotalProcessed = rows.Count();

            // Remove in-file duplicates by (FirstName, LastName, ParentEmail)
            var duplicateGroups = validStudentDtos
                .GroupBy(x => new { x.dto.FirstName, x.dto.LastName, x.dto.ParentEmail })
                .Where(g => g.Count() > 1);

            foreach (var group in duplicateGroups)
            {
                var items = group.ToList();
                for (int i = 1; i < items.Count; i++)
                {
                    result.FailedStudents.Add(new ImportStudentError
                    {
                        RowNumber = items[i].rowNumber,
                        FirstName = items[i].dto.FirstName,
                        LastName = items[i].dto.LastName,
                        ParentEmail = items[i].dto.ParentEmail,
                        ErrorMessage = "Duplicate student found in Excel file."
                    });
                    validStudentDtos.Remove(items[i]);
                }
            }

            // Remove DB duplicates by (FirstName, LastName, ParentEmail)
            foreach (var (dto, rowNumber) in validStudentDtos.ToList())
            {
                var existing = (await _studentRepository.FindByConditionAsync(s =>
                    s.FirstName == dto.FirstName &&
                    s.LastName == dto.LastName &&
                    s.ParentEmail == dto.ParentEmail)).FirstOrDefault();

                if (existing != null)
                {
                    result.FailedStudents.Add(new ImportStudentError
                    {
                        RowNumber = rowNumber,
                        FirstName = dto.FirstName,
                        LastName = dto.LastName,
                        ParentEmail = dto.ParentEmail,
                        ErrorMessage = "Student already exists in database."
                    });
                    validStudentDtos.Remove((dto, rowNumber));
                }
            }

            // Insert rows
            foreach (var (dto, rowNumber) in validStudentDtos)
            {
                try
                {
                    // Try to link Parent by email
                    var parent = (await _parentRepository.FindByConditionAsync(p => p.Email == dto.ParentEmail))
                                 .FirstOrDefault();

                    var student = _mapper.Map<Student>(dto);

                    if (parent != null)
                    {
                        student.ParentId = parent.Id;
                    }
                    else
                    {
                        student.ParentId = null;
                    }

                    // Always keep ParentEmail on Student (requested behavior)
                    student.ParentEmail = dto.ParentEmail;
                    student.Status = StudentStatus.Available;

                    var created = await _studentRepository.AddAsync(student);

                    var success = _mapper.Map<ImportStudentSuccess>(created);
                    success.RowNumber = rowNumber;
                    result.SuccessfulStudents.Add(success);
                }
                catch (Exception ex)
                {
                    result.FailedStudents.Add(new ImportStudentError
                    {
                        RowNumber = rowNumber,
                        FirstName = dto.FirstName,
                        LastName = dto.LastName,
                        ParentEmail = dto.ParentEmail,
                        ErrorMessage = $"Error creating student: {ex.Message}"
                    });
                }
            }

            return result;
        }

        public async Task<byte[]> ExportStudentsToExcelAsync()
        {
            var students = await _studentRepository.FindAllAsync();
            if (students == null || !students.Any())
                return Array.Empty<byte>();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Students");

            // Header
            worksheet.Cell(1, 1).Value = "First Name";
            worksheet.Cell(1, 2).Value = "Last Name";
            worksheet.Cell(1, 3).Value = "Parent Name";
            worksheet.Cell(1, 4).Value = "Parent Email";
            worksheet.Cell(1, 5).Value = "Status";

            int row = 2;
            foreach (var st in students)
            {
                worksheet.Cell(row, 1).Value = st.FirstName;
                worksheet.Cell(row, 2).Value = st.LastName;

                if (st.ParentId.HasValue && st.Parent != null)
                {
                    // Student is linked to a parent
                    worksheet.Cell(row, 3).Value = $"{st.Parent.FirstName} {st.Parent.LastName}";
                    // You can choose either the parent's current email or the student's stored ParentEmail.
                    // Here we show the parent's current email to reflect the authoritative account address.
                    worksheet.Cell(row, 4).Value = st.Parent.Email;
                    worksheet.Cell(row, 5).Value = "Linked";
                }
                else
                {
                    // Student is not linked to a parent
                    worksheet.Cell(row, 3).Value = "Not linked";
                    worksheet.Cell(row, 4).Value = st.ParentEmail; // still kept on Student
                    worksheet.Cell(row, 5).Value = "Pending";
                }
                row++;
            }

            using var stream = new System.IO.MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<StudentDto> ActivateStudentAsync(Guid id)
        {
            var student = await _studentRepository.FindAsync(id);
            if (student == null)
                throw new KeyNotFoundException("Student not found");

            student.Status = StudentStatus.Active;
            student.ActivatedAt = DateTime.UtcNow;
            student.DeactivatedAt = null;
            student.DeactivationReason = null;

            await _studentRepository.UpdateAsync(student);
            return _mapper.Map<StudentDto>(student);
        }

        public async Task<StudentDto> DeactivateStudentAsync(Guid id, string reason)
        {
            var student = await _studentRepository.FindAsync(id);
            if (student == null)
                throw new KeyNotFoundException("Student not found");

            student.Status = StudentStatus.Inactive;
            student.DeactivatedAt = DateTime.UtcNow;
            student.DeactivationReason = reason;

            await _studentRepository.UpdateAsync(student);
            return _mapper.Map<StudentDto>(student);
        }

        public async Task<StudentDto> RestoreStudentAsync(Guid id)
        {
            var student = await _studentRepository.FindAsync(id);
            if (student == null)
                throw new KeyNotFoundException("Student not found");

            if (student.Status == StudentStatus.Deleted)
            {
                student.Status = StudentStatus.Available;
                student.DeactivatedAt = null;
                student.DeactivationReason = null;
            }
            else if (student.Status == StudentStatus.Inactive)
            {
                student.Status = StudentStatus.Active;
                student.DeactivatedAt = null;
                student.DeactivationReason = null;
            }

            await _studentRepository.UpdateAsync(student);
            return _mapper.Map<StudentDto>(student);
        }

        public async Task<StudentDto> SoftDeleteStudentAsync(Guid id, string reason)
        {
            var student = await _studentRepository.FindAsync(id);
            if (student == null)
                throw new KeyNotFoundException("Student not found");

            student.Status = StudentStatus.Deleted;
            student.DeactivatedAt = DateTime.UtcNow;
            student.DeactivationReason = reason;

            await _studentRepository.UpdateAsync(student);
            return _mapper.Map<StudentDto>(student);
        }

        public async Task<IEnumerable<StudentDto>> GetStudentsByStatusAsync(Data.Models.Enums.StudentStatus status)
        {
            if (status == Data.Models.Enums.StudentStatus.Deleted)
            {
                var students = await _studentRepository.GetQueryable()
                    .Where(s => s.Status == status)
                    .ToListAsync();
                return _mapper.Map<IEnumerable<StudentDto>>(students);
            }
            else
            {
                var students = await _studentRepository.FindByConditionAsync(s => s.Status == status && !s.IsDeleted);
                return _mapper.Map<IEnumerable<StudentDto>>(students);
            }
        }

        // TODO: Add payment status logic when payment service is ready
        // This method will be called when payment is processed to auto-activate student
        public async Task<StudentDto> ActivateStudentByPaymentAsync(Guid id)
        {
            var student = await _studentRepository.FindAsync(id);
            if (student == null)
                throw new KeyNotFoundException("Student not found");

            // Auto-activate when payment is made
            if (student.Status == StudentStatus.Available || student.Status == StudentStatus.Pending)
            {
                student.Status = StudentStatus.Active;
                student.ActivatedAt = DateTime.UtcNow;
                await _studentRepository.UpdateAsync(student);
            }

            return _mapper.Map<StudentDto>(student);
        }
    }
}
