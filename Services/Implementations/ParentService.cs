using AutoMapper;
using ClosedXML.Excel;
using Data.Models;
using Data.Repos.Interfaces;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<ParentService> _logger;
        
        public ParentService(IParentRepository parentRepository, IUserAccountRepository userAccountRepository,
            IStudentRepository studentRepository, IMapper mapper, IEmailService emailService, ILogger<ParentService> logger)
        {
            _parentRepository = parentRepository;
            _userAccountRepository = userAccountRepository;
            _studentRepository = studentRepository;
            _mapper = mapper;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<CreateUserResponse> CreateParentAsync(CreateParentRequest dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (await _userAccountRepository.IsEmailExistAsync(dto.Email))
                throw new InvalidOperationException("Email already exists.");
            if (await _userAccountRepository.IsPhoneNumberExistAsync(dto.PhoneNumber))
                throw new InvalidOperationException("Phone number already exists.");

            var parent = _mapper.Map<Parent>(dto);

            var rawPassword = SecurityHelper.GenerateRandomPassword();
            var hashedPassword = SecurityHelper.HashPassword(rawPassword);
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

            try
            {
                await SendWelcomeEmailAsync(createdParent.Email, mailContent.subject, mailContent.body);
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "Failed to send welcome email to {Email}. Account created successfully.", createdParent.Email);
            }
            
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

            // List to collect email tasks for parallel sending
            var emailTasks = new List<(string email, string subject, string body, int rowNumber, string firstName, string lastName)>();

            // Insert each parent sequentially (for database consistency)
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

                    var rawPassword = SecurityHelper.GenerateRandomPassword();
                    parent.HashedPassword = SecurityHelper.HashPassword(rawPassword);

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

                    // Prepare email content but don't send yet
                    var mailContent = CreateWelcomeEmailTemplate(
                        createdParent.FirstName,
                        createdParent.LastName,
                        createdParent.Email,
                        rawPassword);

                    // Add to email tasks list for parallel sending
                    emailTasks.Add((createdParent.Email, mailContent.subject, mailContent.body, rowNumber, createdParent.FirstName, createdParent.LastName));
                    
                    _logger.LogDebug("Added email task for {Email} (Row {RowNumber}). Total email tasks: {Count}", 
                        createdParent.Email, rowNumber, emailTasks.Count);
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
            // Send all emails in parallel after all accounts are created
            if (emailTasks.Count > 0)
            {
                _logger.LogInformation("Preparing to send {Count} welcome emails for imported parents", emailTasks.Count);
                
                var successCount = 0;
                var failureCount = 0;
                
                var emailTasksToRun = emailTasks.Select(async (emailTask) =>
                {
                    try
                    {
                        _logger.LogInformation("Attempting to send email to {Email} (Row {RowNumber})", emailTask.email, emailTask.rowNumber);
                        await SendWelcomeEmailAsync(emailTask.email, emailTask.subject, emailTask.body);
                        Interlocked.Increment(ref successCount);
                        _logger.LogInformation("Successfully sent email to {Email} (Row {RowNumber})", emailTask.email, emailTask.rowNumber);
                    }
                    catch (Exception emailEx)
                    {
                        Interlocked.Increment(ref failureCount);
                        _logger.LogError(emailEx, "Failed to send welcome email to {Email} (Row {RowNumber}). Account created successfully. Error: {ErrorMessage}", 
                            emailTask.email, emailTask.rowNumber, emailEx.Message);
                    }
                }).ToList();

                _logger.LogInformation("Starting parallel email sending for {Count} emails", emailTasksToRun.Count);
                await Task.WhenAll(emailTasksToRun);
                _logger.LogInformation("Completed parallel email sending. Success: {SuccessCount}, Failed: {FailureCount} out of {TotalCount}", 
                    successCount, failureCount, emailTasks.Count);
            }
            else
            {
                _logger.LogWarning("No email tasks to send. Successful users: {Count}", result.SuccessfulUsers.Count);
            }

            return result;
        }

        public async Task<int> LinkStudentsByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return 0;

            var parent = (await _parentRepository.FindByConditionAsync(p => p.Email == email)).FirstOrDefault();
            if (parent == null) return 0;

            var students = await _studentRepository.FindByConditionAsync(
                s => s.ParentId == null && s.ParentEmail == email);

            int updated = 0;
            foreach (var student in students)
            {
                student.ParentId = parent.Id;
                student.ParentEmail = string.Empty;
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
                await _emailService.SendEmailAsync(email, subject, body);
                _logger.LogInformation("Email sent successfully to {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SendWelcomeEmailAsync failed for {Email}: {ErrorMessage}", email, ex.Message);
                throw; // Re-throw to let caller handle
            }
        }
        private void QueueWelcomeEmail(string email, string subject, string body)
        {
            _emailService.QueueEmail(email, subject, body);
        }
        private (string subject, string body) CreateWelcomeEmailTemplate(string firstName, string lastName, string email, string password)
        {
            var subject = "🎉 Thông tin tài khoản phụ huynh EduBus | Parent Account Information";
            
            // Vietnamese version
            var bodyVietnamese = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset=""UTF-8"">
                    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                </head>
                <body style=""margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f5f5f5;"">
                    <div style=""max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 20px;"">
                    <h2 style=""color: #2E7D32; margin-top: 0;"">🎉 Chúc mừng! Tài khoản của bạn đã được tạo thành công</h2>
                    
                    <p>Xin chào <strong>{firstName} {lastName}</strong>,</p>
                    
                    <p>Chúng tôi rất vui thông báo rằng tài khoản phụ huynh của bạn trên hệ thống <strong>EduBus</strong> đã được tạo thành công.</p>
                    
                    <div style=""background-color: #E8F5E8; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #2E7D32;"">
                        <h3 style=""color: #2E7D32; margin-top: 0;"">📄 Thông tin tài khoản của bạn:</h3>
                        <p style=""margin: 10px 0;""><strong>Email đăng nhập:</strong> <a href=""mailto:{email}"" style=""color: #1976D2; text-decoration: none;"">{email}</a></p>
                        <p style=""margin: 10px 0;""><strong>Mật khẩu:</strong> <code style=""background-color: #f5f5f5; padding: 4px 8px; border-radius: 4px; font-family: monospace; font-size: 14px;"">{password}</code></p>
                        <p style=""color: #D32F2F; font-size: 14px; margin-top: 15px;""><strong>⚠️ Lưu ý:</strong> Vui lòng đổi mật khẩu sau lần đăng nhập đầu tiên để bảo mật tài khoản của bạn.</p>
                    </div>
                    
                    <div style=""background-color: #FFF3E0; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #F57C00;"">
                        <h3 style=""color: #F57C00; margin-top: 0;"">📱 Hướng dẫn sử dụng tài khoản:</h3>
                        <ol style=""line-height: 1.8;"">
                            <li><strong>Bước 1:</strong> Đăng nhập vào ứng dụng EduBus bằng email và mật khẩu được cung cấp ở trên</li>
                            <li><strong>Bước 2:</strong> Tiến hành đăng ký dịch vụ đưa đón lần đầu cho con của bạn</li>
                            <li><strong>Bước 3:</strong> Chọn điểm đón phù hợp và xác nhận thông tin</li>
                            <li><strong>Bước 4:</strong> Thanh toán phí dịch vụ theo hướng dẫn trong ứng dụng</li>
                            <li><strong>Bước 5:</strong> Theo dõi lịch trình xe buýt và thông tin vận chuyển của con bạn</li>
                        </ol>
                    </div>
                    
                    <div style=""background-color: #E3F2FD; padding: 15px; border-radius: 8px; margin: 20px 0;"">
                        <p style=""margin: 0; color: #1976D2;""><strong>💡 Mẹo:</strong> Bạn có thể tải ứng dụng EduBus trên điện thoại để quản lý và theo dõi dễ dàng hơn.</p>
                    </div>
                    
                    <p>Nếu bạn gặp bất kỳ khó khăn nào, vui lòng liên hệ bộ phận hỗ trợ của chúng tôi.</p>
                    
                    <p style=""margin-top: 30px;"">Trân trọng,<br>
                    <strong style=""color: #2E7D32;"">Đội ngũ EduBus</strong></p>
                    
                    <hr style=""border: none; border-top: 1px solid #e0e0e0; margin: 30px 0;"">
                    
                    <h2 style=""color: #2E7D32;"">🎉 Congratulations! Your account has been created successfully</h2>
                    
                    <p>Hello <strong>{firstName} {lastName}</strong>,</p>
                    
                    <p>We are pleased to inform you that your parent account on the <strong>EduBus</strong> system has been successfully created.</p>
                    
                    <div style=""background-color: #E8F5E8; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #2E7D32;"">
                        <h3 style=""color: #2E7D32; margin-top: 0;"">📄 Your account details:</h3>
                        <p style=""margin: 10px 0;""><strong>Login email:</strong> <a href=""mailto:{email}"" style=""color: #1976D2; text-decoration: none;"">{email}</a></p>
                        <p style=""margin: 10px 0;""><strong>Password:</strong> <code style=""background-color: #f5f5f5; padding: 4px 8px; border-radius: 4px; font-family: monospace; font-size: 14px;"">{password}</code></p>
                        <p style=""color: #D32F2F; font-size: 14px; margin-top: 15px;""><strong>⚠️ Note:</strong> Please change your password after your first login to keep your account secure.</p>
                    </div>
                    
                    <div style=""background-color: #FFF3E0; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #F57C00;"">
                        <h3 style=""color: #F57C00; margin-top: 0;"">📱 How to use your account:</h3>
                        <ol style=""line-height: 1.8;"">
                            <li><strong>Step 1:</strong> Log in to the EduBus app using the email and password provided above</li>
                            <li><strong>Step 2:</strong> Proceed with the first-time registration of transportation service for your child</li>
                            <li><strong>Step 3:</strong> Select a suitable pickup point and confirm the information</li>
                            <li><strong>Step 4:</strong> Make payment for the service fee as instructed in the app</li>
                            <li><strong>Step 5:</strong> Track the bus schedule and transportation information for your child</li>
                        </ol>
                    </div>
                    
                    <div style=""background-color: #E3F2FD; padding: 15px; border-radius: 8px; margin: 20px 0;"">
                        <p style=""margin: 0; color: #1976D2;""><strong>💡 Tip:</strong> You can download the EduBus app on your phone for easier management and tracking.</p>
                    </div>
                    
                    <p>If you encounter any difficulties, please contact our support team.</p>
                    
                    <p style=""margin-top: 30px;"">Best regards,<br>
                    <strong style=""color: #2E7D32;"">EduBus Team</strong></p>
                    </div>
                </body>
                </html>";

            return (subject, bodyVietnamese);
        }

    }
}
