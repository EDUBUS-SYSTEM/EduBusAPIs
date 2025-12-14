using AutoMapper;
using ClosedXML.Excel;
using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Services.Contracts;
using Services.Models.Parent;
using Services.Models.UserAccount;
using Services.Validators;
using System.Net.Http.Json;
using Utils;
using Constants;

namespace Services.Implementations
{
    public class ParentService : IParentService
    {
        private readonly IParentRepository _parentRepository;
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IEnrollmentSemesterSettingsRepository _semesterRepository;
        private readonly ITransportFeeItemRepository _transportFeeItemRepository;
        private readonly ITripRepository _tripRepository;
        private readonly IAcademicCalendarService _academicCalendarService;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly ILogger<ParentService> _logger;
        private readonly IFaceEmbeddingRepository _faceEmbeddingRepository;
        private readonly IFileService _fileService;
        private readonly IConfiguration _configuration;

        private readonly UserAccountValidationService _validationService;
        public ParentService(IParentRepository parentRepository, IUserAccountRepository userAccountRepository,
            IStudentRepository studentRepository, IMapper mapper, IFaceEmbeddingRepository faceEmbeddingRepository, IFileService fileService, IEmailService emailService, ILogger<ParentService> logger, UserAccountValidationService validationService, IConfiguration configuration, IEnrollmentSemesterSettingsRepository semesterRepository, ITransportFeeItemRepository transportFeeItemRepository, ITripRepository tripRepository, IAcademicCalendarService academicCalendarService)
        {
            _parentRepository = parentRepository;
            _userAccountRepository = userAccountRepository;
            _studentRepository = studentRepository;
            _semesterRepository = semesterRepository;
            _transportFeeItemRepository = transportFeeItemRepository;
            _tripRepository = tripRepository;
            _academicCalendarService = academicCalendarService;
            _mapper = mapper;
            _validationService = validationService;
            _emailService = emailService;
            _logger = logger;
            _faceEmbeddingRepository = faceEmbeddingRepository;
            _fileService = fileService;
            _configuration = configuration;
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

        public async Task<ParentTripReportResponse> GetTripReportBySemesterAsync(string parentEmail, string semesterKey)
        {
            if (string.IsNullOrWhiteSpace(parentEmail))
            {
                throw new ArgumentException("Parent email is required.", nameof(parentEmail));
            }

            if (string.IsNullOrWhiteSpace(semesterKey))
            {
                throw new ArgumentException("Semester key is required.", nameof(semesterKey));
            }

            semesterKey = semesterKey.Trim();

            var parentRecords = await _parentRepository.FindByConditionAsync(p => p.Email == parentEmail);
            if (parentRecords == null || !parentRecords.Any())
            {
                throw new KeyNotFoundException("Parent not found.");
            }

            EnrollmentSemesterSettings? semester = null;

            if (Guid.TryParse(semesterKey, out var semesterGuid))
            {
                semester = await _semesterRepository.FindAsync(semesterGuid);
            }
            else
            {
                semester = await _semesterRepository.FindBySemesterCodeAsync(semesterKey);
                if (semester == null)
                {
                    var activeCalendars = await _academicCalendarService.GetActiveAcademicCalendarsAsync();
                    var matchingSemester = activeCalendars
                        .SelectMany(ac => ac.Semesters.Select(s => new { Semester = s, Calendar = ac }))
                        .FirstOrDefault(x => string.Equals(x.Semester.Code, semesterKey, StringComparison.OrdinalIgnoreCase) && x.Semester.IsActive && x.Calendar.IsActive);

                    if (matchingSemester != null)
                    {
                        semester = new EnrollmentSemesterSettings
                        {
                            Id = Guid.NewGuid(),
                            SemesterName = matchingSemester.Semester.Name,
                            SemesterCode = matchingSemester.Semester.Code,
                            AcademicYear = matchingSemester.Calendar.AcademicYear,
                            SemesterStartDate = matchingSemester.Semester.StartDate,
                            SemesterEndDate = matchingSemester.Semester.EndDate,
                            RegistrationStartDate = matchingSemester.Calendar.StartDate,
                            RegistrationEndDate = matchingSemester.Calendar.EndDate,
                            IsActive = true,
                            Description = matchingSemester.Calendar.Name
                        };
                    }
                }
            }

            if (semester == null || !semester.IsActive)
            {
                throw new KeyNotFoundException("Semester not found or inactive.");
            }

            var students = await _studentRepository.GetStudentsByParentEmailAsync(parentEmail) ?? new List<Student>();

            var allFeeItemsForParent = await _transportFeeItemRepository.GetByParentEmailAsync(parentEmail) ?? new List<TransportFeeItem>();
            var feeItems = allFeeItemsForParent
                .Where(f =>
                    string.Equals(f.AcademicYear?.Trim(), semester.AcademicYear?.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    f.SemesterStartDate <= semester.SemesterEndDate &&
                    f.SemesterEndDate >= semester.SemesterStartDate)
                .ToList();

            var registeredStudentIds = feeItems.Any()
                ? students
                    .Where(s => feeItems.Any(f => f.StudentId == s.Id))
                    .Select(s => s.Id)
                    .Distinct()
                    .ToList()
                : students.Select(s => s.Id).Distinct().ToList();

            var trips = await _tripRepository.GetTripsBySemesterForStudentsAsync(
                registeredStudentIds,
                semester.SemesterStartDate,
                semester.SemesterEndDate);

            var tripList = trips ?? new List<Trip>();

            var response = new ParentTripReportResponse
            {
                SemesterId = semester.Id.ToString(),
                SemesterName = semester.SemesterName,
                SemesterCode = semester.SemesterCode,
                AcademicYear = semester.AcademicYear,
                SemesterStartDate = semester.SemesterStartDate,
                SemesterEndDate = semester.SemesterEndDate,
                TotalStudentsRegistered = registeredStudentIds.Count,
                TotalAmountPaid = feeItems.Where(f => f.Status == TransportFeeItemStatus.Paid).Sum(f => f.Subtotal),
                TotalAmountPending = feeItems.Where(f => f.Status != TransportFeeItemStatus.Paid && f.Status != TransportFeeItemStatus.Cancelled).Sum(f => f.Subtotal),
                TotalTrips = tripList.Count,
                CompletedTrips = tripList.Count(t => t.Status == TripConstants.TripStatus.Completed),
                ScheduledTrips = tripList.Count(t => t.Status == TripConstants.TripStatus.Scheduled),
                CancelledTrips = tripList.Count(t => t.Status == TripConstants.TripStatus.Cancelled)
            };

            var studentStatistics = new List<StudentTripStatistics>();

            foreach (var student in students.Where(s => registeredStudentIds.Contains(s.Id)))
            {
                var studentFees = feeItems.Where(f => f.StudentId == student.Id).ToList();
                var studentTrips = tripList
                    .Where(t => t.Stops.Any(s => s.Attendance.Any(a => a.StudentId == student.Id)))
                    .ToList();

                var statistics = new StudentTripStatistics
                {
                    StudentId = student.Id,
                    StudentName = $"{student.FirstName} {student.LastName}".Trim(),
                    Grade = string.Empty,
                    AmountPaid = studentFees.Where(f => f.Status == TransportFeeItemStatus.Paid).Sum(f => f.Subtotal),
                    AmountPending = studentFees.Where(f => f.Status != TransportFeeItemStatus.Paid && f.Status != TransportFeeItemStatus.Cancelled).Sum(f => f.Subtotal),
                    TotalTripsForStudent = studentTrips.Count,
                    CompletedTripsForStudent = studentTrips.Count(t => t.Status == TripConstants.TripStatus.Completed),
                    UpcomingTripsForStudent = studentTrips.Count(t => t.Status == TripConstants.TripStatus.Scheduled)
                };

                var attendanceRecords = studentTrips
                    .SelectMany(t => t.Stops)
                    .Where(stop => stop.Attendance != null)
                    .SelectMany(stop => stop.Attendance.Where(a => a.StudentId == student.Id));

                foreach (var attendance in attendanceRecords)
                {
                    statistics.TotalAttendanceRecords += 1;

                    var state = attendance.State;

                    var isPresent =
                        state == TripConstants.AttendanceStates.Present ||
                        state == TripConstants.AttendanceStates.Boarded ||
                        state == TripConstants.AttendanceStates.Alighted;

                    if (isPresent)
                    {
                        statistics.PresentCount += 1;
                    }
                    else if (state == TripConstants.AttendanceStates.Absent)
                    {
                        statistics.AbsentCount += 1;
                    }
                }

                if (statistics.TotalAttendanceRecords > 0)
                {
                    statistics.AttendanceRate = Math.Round((double)statistics.PresentCount / statistics.TotalAttendanceRecords * 100, 2);
                }

                studentStatistics.Add(statistics);
            }

            response.StudentStatistics = studentStatistics;

            return response;
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


        public async Task<EnrollChildResponse> EnrollChildAsync(Guid userId, EnrollChildRequest request)
        {
            // 1. Validate parent owns this student
            var parent = await _parentRepository.FindAsync(userId);
            if (parent == null)
                throw new KeyNotFoundException("Parent not found");
            var student = await _studentRepository.FindAsync(request.StudentId);
            if (student == null)
                throw new KeyNotFoundException("Student not found");
            if (student.ParentId != parent.Id)
                throw new ArgumentException("Student does not belong to this parent");
            // 2. Check if student already enrolled
            var existingEmbedding = await _faceEmbeddingRepository.GetByStudentIdAsync(request.StudentId);
            if (existingEmbedding != null)
            {
                _logger.LogWarning("Student {StudentId} already has face embedding", request.StudentId);
                return new EnrollChildResponse
                {
                    Success = false,
                    Message = "Student already enrolled. Please contact support to update.",
                    EmbeddingId = existingEmbedding.Id,
                    PhotosProcessed = 0,
                    AverageQuality = 0
                };
            }
            var embedding = await ExtractEmbeddingsFromPhotosAsync(request.FacePhotos);
            var embeddingJson = System.Text.Json.JsonSerializer.Serialize(embedding);
            // 4. Save to database
            var faceEmbedding = new Data.Models.FaceEmbedding
            {
                StudentId = request.StudentId,
                EmbeddingJson = embeddingJson,
                ModelVersion = Constants.TripConstants.FaceRecognitionConstants.ModelVersions.MobileFaceNet_V1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var savedEmbedding = await _faceEmbeddingRepository.AddAsync(faceEmbedding);
            
            // 5. Save original photos
             _logger.LogInformation("Saving {Count} original photos for enrollment...", request.FacePhotos.Count);
            int photoIdx = 1;
            Guid? firstPhotoId = null;
            
            foreach (var base64Photo in request.FacePhotos)
            {
                try
                {
                    // Clean base64 string if it has prefix
                    var cleanBase64 = base64Photo;
                    if (cleanBase64.Contains(","))
                        cleanBase64 = cleanBase64.Split(',')[1];

                    var bytes = Convert.FromBase64String(cleanBase64);
                    var validFile = new MemoryFormFile(
                        "face_photo.jpg", 
                        $"enrollment_{request.StudentId}_{photoIdx}.jpg", 
                        bytes, 
                        "image/jpeg");

                    // Upload and capture photo ID
                    var uploadedFileId = await _fileService.UploadFileAsync(savedEmbedding.Id, "FaceEmbedding", "EnrollmentPhoto", validFile);
                    
                    // Store first photo ID for response AND in FaceEmbedding
                    if (photoIdx == 1)
                    {
                        firstPhotoId = uploadedFileId;
                        savedEmbedding.FirstPhotoFileId = uploadedFileId;
                        await _faceEmbeddingRepository.UpdateAsync(savedEmbedding);
                        _logger.LogInformation("Captured first photo ID {FileId} for student {StudentId}", uploadedFileId, request.StudentId);
                    }
                    
                    photoIdx++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save enrollment photo {Index} for student {StudentId}", photoIdx, request.StudentId);
                    // Don't fail the whole request just because image save failed, as embedding is arguably more important
                }
            }

            _logger.LogInformation("Successfully enrolled student {StudentId} with face embedding {EmbeddingId}",
                request.StudentId, savedEmbedding.Id);
            return new EnrollChildResponse
            {
                Success = true,
                Message = "Child enrolled successfully",
                EmbeddingId = savedEmbedding.Id,
                PhotosProcessed = request.FacePhotos.Count,
                AverageQuality = 1.0, // Assumed good quality for real photos
                StudentImageId = firstPhotoId
            };
        }

        private async Task<List<float>> ExtractEmbeddingsFromPhotosAsync(List<string> photos)
        {
            using var httpClient = new HttpClient();
            
            var faceExtractionUrl = _configuration["FaceExtraction:Url"] 
                ?? "";
            var timeoutSeconds = _configuration.GetValue<int>("FaceExtraction:TimeoutSeconds", 300);
            
            httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            
            _logger.LogInformation("Calling Face Extraction Service at {Url} with timeout {Timeout}s (photos: {Count})", 
                faceExtractionUrl, timeoutSeconds, photos.Count);
            
            try
            {
                var response = await httpClient.PostAsJsonAsync(faceExtractionUrl, new { photos });
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Face extraction service returned {StatusCode}: {ErrorBody}", 
                        response.StatusCode, errorBody);
                    
                    // Parse quality validation errors
                    if (response.StatusCode == System.Net.HttpStatusCode.BadRequest && !string.IsNullOrEmpty(errorBody))
                    {
                        try
                        {
                            var errorJson = System.Text.Json.JsonDocument.Parse(errorBody);
                            if (errorJson.RootElement.TryGetProperty("error", out var errorMsg))
                            {
                                var userFriendlyError = errorMsg.GetString();
                                if (errorJson.RootElement.TryGetProperty("quality_issue", out var qualityIssue))
                                {
                                    var issue = qualityIssue.GetString();
                                    throw new ArgumentException($"Photo quality issue: {userFriendlyError}");
                                }
                                throw new ArgumentException($"Face extraction failed: {userFriendlyError}");
                            }
                        }
                        catch (System.Text.Json.JsonException)
                        {
                            // If JSON parsing fails, throw generic error
                        }
                    }
                    
                    response.EnsureSuccessStatusCode();
                }
                
                var result = await response.Content.ReadFromJsonAsync<FaceExtractionResult>();
                
                _logger.LogInformation("Successfully extracted face embedding from {Count} photos", photos.Count);
                
                return result?.Embedding ?? throw new Exception("No embedding returned from face extraction service");
            }
            catch (ArgumentException)
            {
                // Re-throw quality validation errors as-is
                throw;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Face extraction service timed out after {Timeout}s. Service may be cold-starting on Render.", timeoutSeconds);
                throw new Exception("Face recognition service is taking too long to respond (cold start). Please try again in a moment.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to connect to face extraction service at {Url}", faceExtractionUrl);
                throw new Exception("Unable to connect to face recognition service. Please try again later.");
            }
        }

        private class FaceExtractionResult
        {
            public List<float> Embedding { get; set; }
        }

        private class MemoryFormFile : IFormFile
        {
            private readonly byte[] _bytes;
            private readonly MemoryStream _stream;

            public MemoryFormFile(string name, string fileName, byte[] bytes, string contentType)
            {
                _bytes = bytes;
                _stream = new MemoryStream(bytes);
                Name = name;
                FileName = fileName;
                Length = bytes.Length;
                ContentType = contentType;
            }

            public string ContentType { get; }
            public string ContentDisposition => $"form-data; name=\"{Name}\"; filename=\"{FileName}\"";
            public IHeaderDictionary Headers => new HeaderDictionary();
            public long Length { get; }
            public string Name { get; }
            public string FileName { get; }

            public void CopyTo(Stream target) => _stream.CopyTo(target);
            public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default) => _stream.CopyToAsync(target, cancellationToken);
            public Stream OpenReadStream() => new MemoryStream(_bytes);
        }

        private List<float> GenerateRandomEmbedding(int dimension)
        {
            var random = new Random();
            var embedding = new List<float>();
            for (int i = 0; i < dimension; i++)
            {
                embedding.Add((float)(random.NextDouble() * 2 - 1)); // Range: -1 to 1
            }
            // Normalize (L2 norm = 1)
            var norm = Math.Sqrt(embedding.Sum(x => x * x));
            return embedding.Select(x => (float)(x / norm)).ToList();
        }

    }
}
