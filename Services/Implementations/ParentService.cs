using AutoMapper;
using ClosedXML.Excel;
using Data.Models;
using Data.Repos.Interfaces;
using Services.Contracts;
using Services.Models.Parent;
using Services.Models.UserAccount;
using Services.Validators;
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
        private readonly UserAccountValidationService _validationService;
        public ParentService(IParentRepository parentRepository, IUserAccountRepository userAccountRepository,
            IStudentRepository studentRepository, IMapper mapper, IEmailService emailService, UserAccountValidationService validationService)
        {
            _parentRepository = parentRepository;
            _userAccountRepository = userAccountRepository;
            _studentRepository = studentRepository;
            _mapper = mapper;
            _validationService = validationService;
            _emailService = emailService;
        }

        public async Task<CreateUserResponse> CreateParentAsync(CreateParentRequest dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            await _validationService.ValidateEmailAndPhoneAsync(dto.Email, dto.PhoneNumber);

            var parent = _mapper.Map<Parent>(dto);

            var (rawPassword, hashedPassword) = SecurityHelper.GenerateAndHashPassword();
            parent.HashedPassword = hashedPassword;

            var createdParent = await _parentRepository.AddAsync(parent);

            var studentsNeedingLink = await _studentRepository.FindByConditionAsync(
                s => s.ParentId == null && s.ParentEmail == dto.Email);

            foreach (var student in studentsNeedingLink)
            {
                student.ParentId = createdParent.Id;
                student.ParentEmail = createdParent.Email;
                await _studentRepository.UpdateAsync(student);
            }

            var response = _mapper.Map<CreateUserResponse>(createdParent);
            response.Password = rawPassword;

            var mailContent = CreateWelcomeEmailTemplate(
                createdParent.FirstName,
                createdParent.LastName,
                createdParent.Email,
                rawPassword);

            await SendWelcomeEmailAsync(createdParent.Email, mailContent.subject, mailContent.body);
            return response;
        }

        public async Task<ImportUsersResult> ImportParentsFromExcelAsync(Stream excelFileStream)
        {
            if (excelFileStream == null)
                throw new ArgumentNullException(nameof(excelFileStream));

            var result = new ImportUsersResult();
            var validParentDtos = new List<(ImportParentDto dto, int rowNumber)>();

            using var workbook = new XLWorkbook(excelFileStream);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            if (worksheet == null)
                throw new InvalidOperationException("Excel file does not contain any worksheets.");

            var rows = worksheet.RangeUsed()?.RowsUsed().Skip(1);
            if (rows == null || !rows.Any())
                throw new InvalidOperationException("Excel file does not contain any data rows.");

            // Parse rows
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
                        RowNumber = row.RowNumber(),
                        Email = "",
                        FirstName = "",
                        LastName = "",
                        ErrorMessage = $"Error parsing data: {ex.Message}"
                    });
                }
            }

            result.TotalProcessed = rows.Count();

            // Insert each parent
            foreach (var (parentDto, rowNumber) in validParentDtos)
            {
                try
                {
                    if (await _userAccountRepository.IsEmailExistAsync(parentDto.Email))
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

                    if (!string.IsNullOrWhiteSpace(parentDto.PhoneNumber))
                    {
                        if (await _userAccountRepository.IsPhoneNumberExistAsync(parentDto.PhoneNumber))
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

                    var parent = _mapper.Map<Parent>(parentDto);

                    var (rawPassword, hashedPassword) = SecurityHelper.GenerateAndHashPassword();
                    parent.HashedPassword = hashedPassword;

                    var createdParent = await _parentRepository.AddAsync(parent);

                    var studentsNeedingLink = await _studentRepository.FindByConditionAsync(
                        s => s.ParentId == null && s.ParentEmail == parentDto.Email);

                    foreach (var student in studentsNeedingLink)
                    {
                        student.ParentId = createdParent.Id;
                        student.ParentEmail = string.Empty;
                        await _studentRepository.UpdateAsync(student);
                    }

                    var successResult = _mapper.Map<ImportUserSuccess>(createdParent);
                    successResult.RowNumber = rowNumber;
                    successResult.Password = rawPassword;
                    result.SuccessfulUsers.Add(successResult);

                    var mailContent = CreateWelcomeEmailTemplate(
                        createdParent.FirstName,
                        createdParent.LastName,
                        createdParent.Email,
                        rawPassword);

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
