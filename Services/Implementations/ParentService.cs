using AutoMapper;
using ClosedXML.Excel;
using Data.Models;
using Data.Repos.Interfaces;
using Services.Contracts;
using Services.Models.Parent;
using Services.Models.UserAccount;
using Utils;

namespace Services.Implementations
{
    public class ParentService : IParentService
    {
        private readonly IParentRepository _parentRepository;
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        public ParentService(IParentRepository parentRepository, IUserAccountRepository userAccountRepository,
            IStudentRepository studentRepository, IMapper mapper, IEmailService emailService)
        {
            _parentRepository = parentRepository;
            _userAccountRepository = userAccountRepository;
            _studentRepository = studentRepository;
            _mapper = mapper;
            _emailService = emailService;
        }

        public async Task<CreateUserResponse> CreateParentAsync(CreateParentRequest dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));
            var isExistingEmail = await _userAccountRepository.IsEmailExistAsync(dto.Email);
            if (isExistingEmail)
                throw new InvalidOperationException("Email already exists.");
            var isExistingPhone = await _userAccountRepository.IsPhoneNumberExistAsync(dto.PhoneNumber);
            if (isExistingPhone)
                throw new InvalidOperationException("Phone number already exists.");
            var parent = _mapper.Map<Parent>(dto);
            var rawPassword = SecurityHelper.GenerateRandomPassword();
            var hashedPassword = SecurityHelper.HashPassword(rawPassword);
            parent.HashedPassword = hashedPassword;
            var createdParent = await _parentRepository.AddAsync(parent);

            // After parent is created, link existing students by phone number
            var studentsNeedingLink = await _studentRepository.FindByConditionAsync(s => s.ParentId == null && s.ParentPhoneNumber == dto.PhoneNumber);
            foreach (var student in studentsNeedingLink)
            {
                student.ParentId = createdParent.Id;
                student.ParentPhoneNumber = string.Empty; // Clear to avoid duplication
                await _studentRepository.UpdateAsync(student);
            }

            var response = _mapper.Map<CreateUserResponse>(createdParent);
            response.Password = rawPassword;
            var mailContent = CreateWelcomeEmailTemplate(createdParent.FirstName, createdParent.LastName, createdParent.Email, rawPassword );
            await SendWelcomeEmailAsync(createdParent.Email, mailContent.subject, mailContent.body);
            return response;
        }

        public async Task<ImportUsersResult> ImportParentsFromExcelAsync(Stream excelFileStream)
        {
            if (excelFileStream == null)
                throw new ArgumentNullException(nameof(excelFileStream));
            var result = new ImportUsersResult();
            var validParentDtos = new List<(ImportParentDto dto, int rowNumber)>();
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
                        var parentDto = new ImportParentDto
                        {
                            Email = row.Cell(1).GetString().Trim(),
                            FirstName = row.Cell(2).GetString().Trim(),
                            LastName = row.Cell(3).GetString().Trim(),
                            PhoneNumber = row.Cell(4).GetString().Trim(),
                            Gender = row.Cell(5).GetValue<int>(),
                            DateOfBirthString = row.Cell(6).GetString().Trim(),
                            Address = row.Cell(7).GetString().Trim()
                        };

                        // Validate basic data
                        if (string.IsNullOrWhiteSpace(parentDto.Email) ||
                            string.IsNullOrWhiteSpace(parentDto.FirstName) ||
                            string.IsNullOrWhiteSpace(parentDto.LastName) ||
                            string.IsNullOrWhiteSpace(parentDto.PhoneNumber) ||
                            parentDto.Gender == 0 ||
                            string.IsNullOrWhiteSpace(parentDto.DateOfBirthString) ||
                            string.IsNullOrWhiteSpace(parentDto.Address))

                        {
                            result.FailedUsers.Add(new ImportUserError
                            {
                                RowNumber = rowNumber,
                                Email = parentDto.Email,
                                FirstName = parentDto.FirstName,
                                LastName = parentDto.LastName,
                                PhoneNumber = parentDto.PhoneNumber,
                                ErrorMessage = "All properties are required."
                            });
                            continue;
                        }

                        // Validate email format
                        if (!EmailHelper.IsValidEmail(parentDto.Email))
                        {
                            result.FailedUsers.Add(new ImportUserError
                            {
                                RowNumber = rowNumber,
                                Email = parentDto.Email,
                                FirstName = parentDto.FirstName,
                                LastName = parentDto.LastName,
                                PhoneNumber = parentDto.PhoneNumber,
                                ErrorMessage = "Invalid email format."
                            });
                            continue;
                        }

                        validParentDtos.Add((parentDto, rowNumber));
                    }
                    catch (Exception ex)
                    {
                        result.FailedUsers.Add(new ImportUserError
                        {
                            RowNumber = rowNumber,
                            Email = "",
                            FirstName = "",
                            LastName = "",
                            ErrorMessage = $"Error parsing data: {ex.Message}"
                        });
                    }
                }
                // Check duplicate emails in Excel file
                var duplicateEmailGroups = validParentDtos
                    .GroupBy(x => x.dto.Email.ToLower())
                    .Where(g => g.Count() > 1);
                foreach (var duplicateGroup in duplicateEmailGroups)
                {
                    var duplicates = duplicateGroup.ToList();
                    // Keep the first record, mark the remaining records as errors
                    for (int i = 1; i < duplicates.Count; i++)
                    {
                        result.FailedUsers.Add(new ImportUserError
                        {
                            RowNumber = duplicates[i].rowNumber,
                            Email = duplicates[i].dto.Email,
                            FirstName = duplicates[i].dto.FirstName,
                            LastName = duplicates[i].dto.LastName,
                            PhoneNumber = duplicates[i].dto.PhoneNumber,
                            ErrorMessage = "Duplicate email found in Excel file."
                        });
                        validParentDtos.Remove(duplicates[i]);
                    }
                }
                //Check duplicate phones in Excel file
                var duplicatePhoneGroups = validParentDtos
                    .Where(x => !string.IsNullOrWhiteSpace(x.dto.PhoneNumber))
                    .GroupBy(x => x.dto.PhoneNumber)
                    .Where(g => g.Count() > 1);
                foreach (var duplicateGroup in duplicatePhoneGroups)
                {
                    var duplicates = duplicateGroup.ToList();
                    // Keep the first record, mark the remaining records as errors
                    for (int i = 1; i < duplicates.Count; i++)
                    {
                        result.FailedUsers.Add(new ImportUserError
                        {
                            RowNumber = duplicates[i].rowNumber,
                            Email = duplicates[i].dto.Email,
                            FirstName = duplicates[i].dto.FirstName,
                            LastName = duplicates[i].dto.LastName,
                            PhoneNumber = duplicates[i].dto.PhoneNumber,
                            ErrorMessage = "Duplicate phone number found in Excel file."
                        });
                        validParentDtos.Remove(duplicates[i]);
                    }
                }
                // Set total number of records processed
                result.TotalProcessed = rows.Count();

                // Process từng parent
                foreach (var (parentDto, rowNumber) in validParentDtos)
                {
                    try
                    {
                        // Check email exists in database
                        var isExistingEmail = await _userAccountRepository.IsEmailExistAsync(parentDto.Email);
                        if (isExistingEmail)
                        {
                            result.FailedUsers.Add(new ImportUserError
                            {
                                RowNumber = rowNumber,
                                Email = parentDto.Email,
                                FirstName = parentDto.FirstName,
                                LastName = parentDto.LastName,
                                PhoneNumber = parentDto.PhoneNumber,
                                ErrorMessage = "Email already exists in database."
                            });
                            continue;
                        }

                        // Check if phone number exists in database
                        if (!string.IsNullOrWhiteSpace(parentDto.PhoneNumber))
                        {
                            var isExistingPhone = await _userAccountRepository.IsPhoneNumberExistAsync(parentDto.PhoneNumber);
                            if (isExistingPhone)
                            {
                                result.FailedUsers.Add(new ImportUserError
                                {
                                    RowNumber = rowNumber,
                                    Email = parentDto.Email,
                                    FirstName = parentDto.FirstName,
                                    LastName = parentDto.LastName,
                                    PhoneNumber = parentDto.PhoneNumber,
                                    ErrorMessage = "Phone number already exists in database."
                                });
                                continue;
                            }
                        }

                        // Map DTO thành entity
                        var parent = _mapper.Map<Parent>(parentDto);

                        // Generate password
                        var rawPassword = SecurityHelper.GenerateRandomPassword();
                        var hashedPassword = SecurityHelper.HashPassword(rawPassword);
                        parent.HashedPassword = hashedPassword;

                        var createdParent = await _parentRepository.AddAsync(parent);
                        
                        // After parent is created, link existing students by phone number
                        var studentsNeedingLink = await _studentRepository.FindByConditionAsync(s => s.ParentId == null && s.ParentPhoneNumber == parentDto.PhoneNumber);
                        foreach (var student in studentsNeedingLink)
                        {
                            student.ParentId = createdParent.Id;
                            student.ParentPhoneNumber = string.Empty; // Clear to avoid duplication
                            await _studentRepository.UpdateAsync(student);
                        }
                        
                        var successResult = _mapper.Map<ImportUserSuccess>(createdParent);
                        successResult.RowNumber = rowNumber;
                        successResult.Password = rawPassword;
                        result.SuccessfulUsers.Add(successResult);
                        // send mail
                        var mailContent = CreateWelcomeEmailTemplate(createdParent.FirstName, createdParent.LastName, createdParent.Email, rawPassword);
                        QueueWelcomeEmail(createdParent.Email, mailContent.subject, mailContent.body);
                    }
                    catch (Exception ex)
                    {
                        result.FailedUsers.Add(new ImportUserError
                        {
                            RowNumber = rowNumber,
                            Email = parentDto.Email,
                            FirstName = parentDto.FirstName,
                            LastName = parentDto.LastName,
                            PhoneNumber = parentDto.PhoneNumber,
                            ErrorMessage = $"Error creating parent: {ex.Message}"
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

        public async Task<int> LinkStudentsByPhoneNumberAsync(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber)) return 0;
            var parent = (await _parentRepository.FindByConditionAsync(p => p.PhoneNumber == phoneNumber)).FirstOrDefault();
            if (parent == null) return 0;
            var students = await _studentRepository.FindByConditionAsync(s => s.ParentId == null && s.ParentPhoneNumber == phoneNumber);
            int updated = 0;
            foreach (var student in students)
            {
                student.ParentId = parent.Id;
                student.ParentPhoneNumber = string.Empty; // Clear to avoid duplication
                await _studentRepository.UpdateAsync(student);
                updated++;
            }
            return updated;
        }

        public async Task<byte[]> ExportParentsToExcelAsync()
        {
            var parents = await _parentRepository.FindAllAsync(p => p.Students);
            if (parents == null || !parents.Any())
            {
                return Array.Empty<byte>();
            }
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Parents");

                // Header
                worksheet.Cell(1, 1).Value = "First Name";
                worksheet.Cell(1, 2).Value = "Last Name";
                worksheet.Cell(1, 3).Value = "Email";
                worksheet.Cell(1, 4).Value = "Phone";
                worksheet.Cell(1, 5).Value = "Gender";
                worksheet.Cell(1, 6).Value = "Date of Birth";
                worksheet.Cell(1, 7).Value = "Address";
                worksheet.Cell(1, 8).Value = "Students Count";

                int row = 2;
                foreach (var p in parents)
                {    
                    worksheet.Cell(row, 1).Value = p.FirstName;
                    worksheet.Cell(row, 2).Value = p.LastName;
                    worksheet.Cell(row, 3).Value = p.Email;
                    worksheet.Cell(row, 4).Value = p.PhoneNumber;
                    worksheet.Cell(row, 5).Value = p.Gender.ToString();
                    worksheet.Cell(row, 6).Value = p.DateOfBirth?.ToString("dd/MM/yyyy");
                    worksheet.Cell(row, 7).Value = p.Address;
                    worksheet.Cell(row, 8).Value = p.Students?.Count ?? 0;
                    row++;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }
        private async Task SendWelcomeEmailAsync(string email, string subject, string body)
        {
            try
            {
                Console.WriteLine($"Sending welcome email to: {email}");
                await _emailService.SendEmailAsync(email, subject, body);
                Console.WriteLine($"Welcome email sent successfully to: {email}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send welcome email to {email}: {ex.Message}");
                // Don't throw - email failure shouldn't prevent user creation
                // But log it for monitoring
            }
        }
        private void QueueWelcomeEmail(string email, string subject, string body)
        {
            _emailService.QueueEmail(email, subject, body);
        }
        private (string subject, string body) CreateWelcomeEmailTemplate(string firstName, string lastName, string email, string password)
        {
            var subject = "Thông tin tài khoản phụ huynh EduBus";

            var body = $@"
            <html>
                <body style=""font-family:Arial, Helvetica, sans-serif; font-size:14px; color:#333;"">
                    <p>Xin chào <b>{firstName} {lastName}</b>,</p>
                    <p>Tài khoản phụ huynh của bạn trên hệ thống <b>EduBus</b> đã được khởi tạo thành công. 
                        Dưới đây là thông tin đăng nhập của bạn:</p>
                    <ul>
                        <li><b>Email đăng nhập:</b> {email}</li>
                        <li><b>Mật khẩu tạm thời:</b> {password}</li>
                    </ul>
                    <p>Vui lòng đăng nhập và <b>đổi mật khẩu</b> ngay lần đầu sử dụng để bảo mật tài khoản.</p>
                    <p>Nếu bạn gặp khó khăn, vui lòng liên hệ bộ phận hỗ trợ EduBus.</p>
                    <p>Trân trọng,<br/><b>Đội ngũ EduBus</b></p>
                </body>
            </html>";

            return (subject, body);
        }

    }
}
