using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AutoMapper;
using ClosedXML.Excel;
using Data.Models;
using Data.Repos.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Services.Contracts;
using Services.Models.Supervisor;
using Services.Models.UserAccount;
using Services.Validators;
using Utils;

namespace Services.Implementations
{
    public class SupervisorService : ISupervisorService
    {
        private readonly ISupervisorRepository _supervisorRepository;
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly ILogger<SupervisorService> _logger;
        private readonly UserAccountValidationService _validationService;
        private readonly DbContext _dbContext;

        public SupervisorService(
            ISupervisorRepository supervisorRepository,
            IUserAccountRepository userAccountRepository,
            IMapper mapper,
            IEmailService emailService,
            ILogger<SupervisorService> logger,
            UserAccountValidationService validationService,
            DbContext dbContext)
        {
            _supervisorRepository = supervisorRepository;
            _userAccountRepository = userAccountRepository;
            _mapper = mapper;
            _emailService = emailService;
            _logger = logger;
            _validationService = validationService;
            _dbContext = dbContext;
        }

        public async Task<CreateUserResponse> CreateSupervisorAsync(CreateSupervisorRequest dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            await _validationService.ValidateEmailAndPhoneAsync(dto.Email, dto.PhoneNumber);

            var supervisor = _mapper.Map<Supervisor>(dto);

            var (rawPassword, hashedPassword) = SecurityHelper.GenerateAndHashPassword();
            supervisor.HashedPassword = hashedPassword;

            var createdSupervisor = await _supervisorRepository.AddAsync(supervisor);

            var response = _mapper.Map<CreateUserResponse>(createdSupervisor);
            response.Password = rawPassword;

            var mailContent = CreateWelcomeEmailTemplate(
                createdSupervisor.FirstName,
                createdSupervisor.LastName,
                createdSupervisor.Email,
                rawPassword);

            try
            {
                await SendWelcomeEmailAsync(createdSupervisor.Email, mailContent.subject, mailContent.body);
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx,
                    "Failed to send welcome email to supervisor {Email}. Account created successfully.",
                    createdSupervisor.Email);
            }

            return response;
        }

        public async Task<ImportUsersResult> ImportSupervisorsFromExcelAsync(Stream excelFileStream)
        {
            if (excelFileStream == null)
                throw new ArgumentNullException(nameof(excelFileStream));

            var result = new ImportUsersResult();
            var validSupervisorDtos = new List<(ImportSupervisorDto dto, int rowNumber)>();

            using var workbook = new XLWorkbook(excelFileStream);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            if (worksheet == null)
                throw new InvalidOperationException("Excel file does not contain any worksheets.");

            var rows = worksheet.RangeUsed()?.RowsUsed().Skip(1);
            if (rows == null || !rows.Any())
                throw new InvalidOperationException("Excel file does not contain any data rows.");

            foreach (var row in rows)
            {
                var rowNumber = row.RowNumber();
                try
                {
                    // Map columns according to export format: First Name, Last Name, Email, Phone, Gender, Date of Birth, Address
                    var genderCellValue = row.Cell(5).GetString().Trim();
                    var (isGenderValid, parsedGender, genderErrorMessage) = TryParseGenderValue(genderCellValue);

                    if (!isGenderValid)
                    {
                        result.FailedUsers.Add(new ImportUserError
                        {
                            RowNumber = rowNumber,
                            Email = row.Cell(3).GetString().Trim(),
                            FirstName = row.Cell(1).GetString().Trim(),
                            LastName = row.Cell(2).GetString().Trim(),
                            PhoneNumber = row.Cell(4).GetString().Trim(),
                            ErrorMessage = genderErrorMessage ?? "Invalid gender value. Allowed values: 1 (Male), 2 (Female), 3 (Other) or their text equivalents."
                        });
                        continue;
                    }

                    var supervisorDto = new ImportSupervisorDto
                    {
                        FirstName = row.Cell(1).GetString().Trim(),
                        LastName = row.Cell(2).GetString().Trim(),
                        Email = row.Cell(3).GetString().Trim(),
                        PhoneNumber = row.Cell(4).GetString().Trim(),
                        Gender = parsedGender,
                        DateOfBirthString = row.Cell(6).GetString().Trim(),
                        Address = row.Cell(7).GetString().Trim()
                    };

                    // Validate required fields with detailed error messages
                    var missingFields = new List<string>();
                    if (string.IsNullOrWhiteSpace(supervisorDto.FirstName))
                        missingFields.Add("First Name");
                    if (string.IsNullOrWhiteSpace(supervisorDto.LastName))
                        missingFields.Add("Last Name");
                    if (string.IsNullOrWhiteSpace(supervisorDto.Email))
                        missingFields.Add("Email");
                    if (string.IsNullOrWhiteSpace(supervisorDto.PhoneNumber))
                        missingFields.Add("Phone");
                    if (supervisorDto.Gender == 0)
                        missingFields.Add("Gender");
                    if (string.IsNullOrWhiteSpace(supervisorDto.DateOfBirthString))
                        missingFields.Add("Date of Birth");
                    if (string.IsNullOrWhiteSpace(supervisorDto.Address))
                        missingFields.Add("Address");

                    if (missingFields.Any())
                    {
                        result.FailedUsers.Add(new ImportUserError
                        {
                            RowNumber = rowNumber,
                            Email = supervisorDto.Email ?? "",
                            FirstName = supervisorDto.FirstName ?? "",
                            LastName = supervisorDto.LastName ?? "",
                            PhoneNumber = supervisorDto.PhoneNumber ?? "",
                            ErrorMessage = $"Missing required fields: {string.Join(", ", missingFields)}."
                        });
                        continue;
                    }

                    if (!EmailHelper.IsValidEmail(supervisorDto.Email))
                    {
                        result.FailedUsers.Add(new ImportUserError
                        {
                            RowNumber = rowNumber,
                            Email = supervisorDto.Email,
                            FirstName = supervisorDto.FirstName,
                            LastName = supervisorDto.LastName,
                            PhoneNumber = supervisorDto.PhoneNumber,
                            ErrorMessage = $"Email '{supervisorDto.Email}' is not in a valid format."
                        });
                        continue;
                    }

                    validSupervisorDtos.Add((supervisorDto, rowNumber));
                }
                catch (Exception ex)
                {
                    result.FailedUsers.Add(new ImportUserError
                    {
                        RowNumber = row.RowNumber(),
                        Email = "",
                        FirstName = "",
                        LastName = "",
                        ErrorMessage = $"Error while reading data for row {rowNumber}: {ex.Message}"
                    });
                }
            }

            result.TotalProcessed = rows.Count();

            var emailTasks = new List<(string email, string subject, string body, int rowNumber, string firstName, string lastName)>();

            foreach (var (supervisorDto, rowNumber) in validSupervisorDtos)
            {
                try
                {
                    // Check email duplication with detailed error message
                    var existingUserByEmail = await _userAccountRepository.GetByEmailAsync(supervisorDto.Email);
                    if (existingUserByEmail != null)
                    {
                        var userType = await GetUserTypeAsync(existingUserByEmail);
                        var userFullName = $"{existingUserByEmail.FirstName} {existingUserByEmail.LastName}".Trim();
                        result.FailedUsers.Add(new ImportUserError
                        {
                            RowNumber = rowNumber,
                            Email = supervisorDto.Email,
                            FirstName = supervisorDto.FirstName,
                            LastName = supervisorDto.LastName,
                            PhoneNumber = supervisorDto.PhoneNumber,
                            ErrorMessage = $"Email '{supervisorDto.Email}' already exists in the system for {userType} '{userFullName}' (ID: {existingUserByEmail.Id})."
                        });
                        continue;
                    }

                    // Check phone number duplication with detailed error message
                    if (!string.IsNullOrWhiteSpace(supervisorDto.PhoneNumber))
                    {
                        var existingUserByPhone = await _userAccountRepository.GetByPhoneNumberAsync(supervisorDto.PhoneNumber);
                        if (existingUserByPhone != null)
                        {
                            var userType = await GetUserTypeAsync(existingUserByPhone);
                            var userFullName = $"{existingUserByPhone.FirstName} {existingUserByPhone.LastName}".Trim();
                            result.FailedUsers.Add(new ImportUserError
                            {
                                RowNumber = rowNumber,
                                Email = supervisorDto.Email,
                                FirstName = supervisorDto.FirstName,
                                LastName = supervisorDto.LastName,
                                PhoneNumber = supervisorDto.PhoneNumber,
                            ErrorMessage = $"Phone number '{supervisorDto.PhoneNumber}' already exists in the system for {userType} '{userFullName}' (Email: {existingUserByPhone.Email}, ID: {existingUserByPhone.Id})."
                            });
                            continue;
                        }
                    }

                    var supervisor = _mapper.Map<Supervisor>(supervisorDto);

                    var (rawPassword, hashedPassword) = SecurityHelper.GenerateAndHashPassword();
                    supervisor.HashedPassword = hashedPassword;

                    var createdSupervisor = await _supervisorRepository.AddAsync(supervisor);

                    var successResult = _mapper.Map<ImportUserSuccess>(createdSupervisor);
                    successResult.RowNumber = rowNumber;
                    successResult.Password = rawPassword;
                    result.SuccessfulUsers.Add(successResult);

                    var mailContent = CreateWelcomeEmailTemplate(
                        createdSupervisor.FirstName,
                        createdSupervisor.LastName,
                        createdSupervisor.Email,
                        rawPassword);

                    emailTasks.Add((createdSupervisor.Email, mailContent.subject, mailContent.body, rowNumber, createdSupervisor.FirstName, createdSupervisor.LastName));
                }
                catch (Exception ex)
                {
                    result.FailedUsers.Add(new ImportUserError
                    {
                        RowNumber = rowNumber,
                        Email = supervisorDto.Email,
                        FirstName = supervisorDto.FirstName,
                        LastName = supervisorDto.LastName,
                        PhoneNumber = supervisorDto.PhoneNumber,
                        ErrorMessage = $"Error while creating supervisor: {ex.Message}"
                    });
                }
            }

            if (emailTasks.Count > 0)
            {
                _logger.LogInformation("Preparing to send {Count} welcome emails for imported supervisors", emailTasks.Count);

                var successCount = 0;
                var failureCount = 0;

                var pendingTasks = emailTasks.Select(async task =>
                {
                    try
                    {
                        await SendWelcomeEmailAsync(task.email, task.subject, task.body);
                        Interlocked.Increment(ref successCount);
                        _logger.LogInformation("Successfully sent supervisor welcome email to {Email} (Row {RowNumber})", task.email, task.rowNumber);
                    }
                    catch (Exception emailEx)
                    {
                        Interlocked.Increment(ref failureCount);
                        _logger.LogError(emailEx,
                            "Failed to send supervisor welcome email to {Email} (Row {RowNumber}). Account created successfully.",
                            task.email,
                            task.rowNumber);
                    }
                }).ToList();

                await Task.WhenAll(pendingTasks);
                _logger.LogInformation(
                    "Completed supervisor welcome email sending. Success: {SuccessCount}, Failed: {FailureCount} out of {TotalCount}",
                    successCount,
                    failureCount,
                    emailTasks.Count);
            }
            else
            {
                _logger.LogWarning("No supervisor welcome emails to send. Successful supervisors: {Count}", result.SuccessfulUsers.Count);
            }

            return result;
        }

        public async Task<byte[]> ExportSupervisorsToExcelAsync()
        {
            var supervisors = await _supervisorRepository.FindAllAsync();
            if (supervisors == null || !supervisors.Any())
            {
                return Array.Empty<byte>();
            }
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Supervisors");

                worksheet.Cell(1, 1).Value = "First Name";
                worksheet.Cell(1, 2).Value = "Last Name";
                worksheet.Cell(1, 3).Value = "Email";
                worksheet.Cell(1, 4).Value = "Phone";
                worksheet.Cell(1, 5).Value = "Gender";
                worksheet.Cell(1, 6).Value = "Date of Birth";
                worksheet.Cell(1, 7).Value = "Address";

                int row = 2;
                foreach (var s in supervisors)
                {
                    worksheet.Cell(row, 1).Value = s.FirstName;
                    worksheet.Cell(row, 2).Value = s.LastName;
                    worksheet.Cell(row, 3).Value = s.Email;
                    worksheet.Cell(row, 4).Value = s.PhoneNumber;
                    worksheet.Cell(row, 5).Value = s.Gender.ToString();
                    worksheet.Cell(row, 6).Value = s.DateOfBirth?.ToString("dd/MM/yyyy");
                    worksheet.Cell(row, 7).Value = s.Address;
                    row++;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        public async Task<SupervisorResponse?> GetSupervisorResponseByIdAsync(Guid supervisorId)
        {
            var supervisor = await _supervisorRepository.FindAsync(supervisorId);
            if (supervisor == null || supervisor.IsDeleted)
                return null;

            return _mapper.Map<SupervisorResponse>(supervisor);
        }

        public async Task<IEnumerable<SupervisorResponse>> GetAllSupervisorsAsync()
        {
            var supervisors = await _supervisorRepository.FindAllAsync();
            return _mapper.Map<IEnumerable<SupervisorResponse>>(supervisors);
        }

        /// <summary>
        /// Helper method to determine user type for error messages
        /// </summary>
        private async Task<string> GetUserTypeAsync(UserAccount user)
        {
            // Check if user is Supervisor
            var supervisor = await _dbContext.Set<Supervisor>()
                .FirstOrDefaultAsync(s => s.Id == user.Id && !s.IsDeleted);
            if (supervisor != null) return "Supervisor";

            // Check if user is Driver
            var driver = await _dbContext.Set<Driver>()
                .FirstOrDefaultAsync(d => d.Id == user.Id && !d.IsDeleted);
            if (driver != null) return "Driver";

            // Check if user is Parent
            var parent = await _dbContext.Set<Parent>()
                .FirstOrDefaultAsync(p => p.Id == user.Id && !p.IsDeleted);
            if (parent != null) return "Parent";

            // Check if user is Admin
            var admin = await _dbContext.Set<Admin>()
                .FirstOrDefaultAsync(a => a.Id == user.Id && !a.IsDeleted);
            if (admin != null) return "Admin";

            return "User";
        }

        private static (bool IsValid, int ParsedGender, string? ErrorMessage) TryParseGenderValue(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return (true, 0, null);
            }

            if (int.TryParse(rawValue, out var genderAsInt))
            {
                return (true, genderAsInt, null);
            }

            if (string.Equals(rawValue, "male", StringComparison.OrdinalIgnoreCase))
                return (true, 1, null);

            if (string.Equals(rawValue, "female", StringComparison.OrdinalIgnoreCase))
                return (true, 2, null);

            if (string.Equals(rawValue, "other", StringComparison.OrdinalIgnoreCase))
                return (true, 3, null);

            return (false, 0, $"Invalid gender value '{rawValue}'. Allowed values: 1 (Male), 2 (Female), 3 (Other) or their text equivalents.");
        }

        private async Task SendWelcomeEmailAsync(string email, string subject, string body)
        {
            await _emailService.SendEmailAsync(email, subject, body);
        }

        private (string subject, string body) CreateWelcomeEmailTemplate(string firstName, string lastName, string email, string password)
        {
            var subject = "🎉 EduBus Supervisor Account Created Successfully";
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""margin:0;padding:0;font-family:Arial,sans-serif;background-color:#f5f5f5;"">
    <div style=""max-width:600px;margin:0 auto;background-color:#ffffff;padding:24px;border-radius:8px;"">
        <h2 style=""color:#1565C0;margin-top:0;"">Xin chào {firstName} {lastName},</h2>
        <p>Bạn đã được cấp tài khoản <strong>Supervisor</strong> trên hệ thống <strong>EduBus</strong>.</p>
        <div style=""background-color:#E3F2FD;padding:16px;border-left:4px solid #1565C0;margin:24px 0;border-radius:6px;"">
            <p style=""margin:8px 0;""><strong>Email đăng nhập:</strong> {email}</p>
            <p style=""margin:8px 0;""><strong>Mật khẩu tạm thời:</strong> <code style=""background:#f5f5f5;padding:4px 8px;border-radius:4px;"">{password}</code></p>
            <p style=""color:#D84315;font-size:14px;margin-top:16px;"">Vui lòng đổi mật khẩu sau lần đăng nhập đầu tiên để đảm bảo bảo mật.</p>
        </div>
        <p>Bạn có thể đăng nhập để quản lý chuyến xe, giám sát học sinh và cập nhật thông tin vận hành.</p>
        <hr style=""border:none;border-top:1px solid #e0e0e0;margin:32px 0;"">
        <h3 style=""color:#1565C0;margin-top:0;"">English</h3>
        <p>Hello {firstName} {lastName},</p>
        <p>Your <strong>Supervisor</strong> account on <strong>EduBus</strong> has been created.</p>
        <p>Please log in using the credentials above and change your password after the first login.</p>
        <p style=""margin-top:32px;"">Best regards,<br/><strong>EduBus Team</strong></p>
    </div>
</body>
</html>";

            return (subject, body);
        }
    }
}

