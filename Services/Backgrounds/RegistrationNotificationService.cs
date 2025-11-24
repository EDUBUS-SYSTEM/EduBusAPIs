using System.Threading;
using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Services.Contracts;
using Services.Models.Notification;

namespace Services.Backgrounds
{
    public class RegistrationNotificationService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RegistrationNotificationService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);
        private const int NotificationHour = 8;
        private const int NotificationMinute = 00;
        
        private static TimeZoneInfo GetVietnamTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            }
            catch
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
                }
                catch
                {
                    return TimeZoneInfo.CreateCustomTimeZone("Vietnam Standard Time", TimeSpan.FromHours(7), 
                        "Vietnam Standard Time", "Vietnam Standard Time");
                }
            }
        }
        
        private static readonly TimeZoneInfo VietnamTimeZone = GetVietnamTimeZone();

        public RegistrationNotificationService(
            IServiceScopeFactory scopeFactory,
            ILogger<RegistrationNotificationService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task TriggerNotificationProcessingAsync()
        {
            _logger.LogWarning("Manual notification trigger requested at {Time}", DateTime.UtcNow);
            using var scope = _scopeFactory.CreateScope();
            await ProcessRegistrationNotificationsAsync(scope.ServiceProvider);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var utcNow = DateTime.UtcNow;
            var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, VietnamTimeZone);
            _logger.LogInformation("RegistrationNotificationService started at UTC: {UtcTime}, Vietnam: {VietnamTime}", utcNow, vietnamNow);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    await ProcessRegistrationNotificationsAsync(scope.ServiceProvider);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in RegistrationNotificationService");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("RegistrationNotificationService stopped at: {time}", DateTime.UtcNow);
        }

        private async Task ProcessRegistrationNotificationsAsync(IServiceProvider serviceProvider)
        {
            try
            {
                var utcNow = DateTime.UtcNow;
                var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, VietnamTimeZone);
                
                if (vietnamNow.Hour != NotificationHour || vietnamNow.Minute != NotificationMinute)
                {
                    return;
                }

                var enrollmentRepo = serviceProvider.GetRequiredService<IEnrollmentSemesterSettingsRepository>();
                var userAccountRepo = serviceProvider.GetRequiredService<IUserAccountRepository>();
                var emailService = serviceProvider.GetRequiredService<IEmailService>();
                var notificationService = serviceProvider.GetRequiredService<INotificationService>();

                var allSettings = await enrollmentRepo.FindByFilterAsync(
                    Builders<EnrollmentSemesterSettings>.Filter.And(
                        Builders<EnrollmentSemesterSettings>.Filter.Eq(x => x.IsActive, true),
                        Builders<EnrollmentSemesterSettings>.Filter.Eq(x => x.IsDeleted, false)
                    )
                );

                var today = vietnamNow.Date;
                var fiveDaysFromNow = today.AddDays(5);

                if (!allSettings.Any())
                {
                    _logger.LogInformation("No active enrollment semester settings found");
                    return;
                }

                foreach (var settings in allSettings)
                {
                    try
                    {
                        if (settings.RegistrationStartDate.Date == today)
                        {
                            await SendRegistrationStartNotificationsAsync(
                                settings, userAccountRepo, emailService, notificationService);
                        }

                        if (settings.RegistrationEndDate.Date == fiveDaysFromNow)
                        {
                            await SendRegistrationReminderNotificationsAsync(
                                settings, userAccountRepo, emailService, notificationService);
                        }

                        if (settings.RegistrationEndDate.Date == today)
                        {
                            await SendRegistrationEndNotificationsAsync(
                                settings, userAccountRepo, emailService, notificationService);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing notifications for semester {SemesterCode}", 
                            settings.SemesterCode);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessRegistrationNotificationsAsync");
            }
        }

        private async Task SendRegistrationStartNotificationsAsync(
            EnrollmentSemesterSettings settings,
            IUserAccountRepository userAccountRepo,
            IEmailService emailService,
            INotificationService notificationService)
        {
            var notificationKey = $"registration_start_{settings.Id}_{settings.RegistrationStartDate:yyyyMMdd}";
            
            var parents = await userAccountRepo.GetActiveParentUsersAsync();
            
            if (!parents.Any())
            {
                _logger.LogInformation("Registration start: No active parent accounts found");
                return;
            }

            var parentList = parents.ToList();
            int successCount = 0;
            int skippedCount = 0;
            int errorCount = 0;

            var tasks = parentList.Select(async parent =>
            {
                try
                {
                    var existingNotification = await notificationService.GetNotificationByMetadataAsync(
                        parent.Id, "EnrollmentSemesterSettings", notificationKey);

                    if (existingNotification != null)
                    {
                        Interlocked.Increment(ref skippedCount);
                        return;
                    }

                    if (!string.IsNullOrWhiteSpace(parent.Email))
                    {
                        var (subject, body) = CreateRegistrationStartEmailTemplate(
                            parent.FirstName, parent.LastName, settings);
                        emailService.QueueEmail(parent.Email, subject, body);
                    }

                    var notificationDto = new CreateNotificationDto
                    {
                        UserId = parent.Id,
                        Title = "Đơn đăng ký cho kỳ học mới đã bắt đầu | New Semester Registration Started",
                        Message = $"Đơn đăng ký cho kỳ học {settings.SemesterName} ({settings.AcademicYear}) đã bắt đầu. Vui lòng đăng ký sớm để đảm bảo chỗ cho con bạn. | Registration for {settings.SemesterName} ({settings.AcademicYear}) has started. Please register early to secure a spot for your child.",
                        NotificationType = NotificationType.EnrollmentRegistration,
                        RecipientType = RecipientType.Parent,
                        RelatedEntityId = settings.Id,
                        RelatedEntityType = "EnrollmentSemesterSettings",
                        Metadata = new Dictionary<string, object>
                        {
                            { "semesterCode", settings.SemesterCode },
                            { "semesterName", settings.SemesterName },
                            { "academicYear", settings.AcademicYear },
                            { "registrationStartDate", settings.RegistrationStartDate.ToString("yyyy-MM-dd") },
                            { "registrationEndDate", settings.RegistrationEndDate.ToString("yyyy-MM-dd") },
                            { "notificationKey", notificationKey },
                            { "notificationType", "registration_start" }
                        }
                    };

                    await notificationService.CreateNotificationAsync(notificationDto);
                    Interlocked.Increment(ref successCount);
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref errorCount);
                    _logger.LogError(ex, "Error sending registration start notification to parent {ParentId}",
                        parent.Id);
                }
            });

            await Task.WhenAll(tasks);
            _logger.LogInformation("Registration start notifications - Success: {SuccessCount}, Skipped: {SkippedCount}, Errors: {ErrorCount}",
                successCount, skippedCount, errorCount);
        }

        private async Task SendRegistrationReminderNotificationsAsync(
            EnrollmentSemesterSettings settings,
            IUserAccountRepository userAccountRepo,
            IEmailService emailService,
            INotificationService notificationService)
        {
            var notificationKey = $"registration_reminder_{settings.Id}_{settings.RegistrationEndDate:yyyyMMdd}";
            
            var parents = await userAccountRepo.GetActiveParentUsersAsync();
            
            if (!parents.Any())
            {
                _logger.LogInformation("Registration reminder: No active parent accounts found");
                return;
            }

            var parentList = parents.ToList();
            int successCount = 0;
            int skippedCount = 0;
            int errorCount = 0;

            var tasks = parentList.Select(async parent =>
            {
                try
                {
                    var existingNotification = await notificationService.GetNotificationByMetadataAsync(
                        parent.Id, "EnrollmentSemesterSettings", notificationKey);

                    if (existingNotification != null)
                    {
                        Interlocked.Increment(ref skippedCount);
                        return;
                    }

                    if (!string.IsNullOrWhiteSpace(parent.Email))
                    {
                        var (subject, body) = CreateRegistrationReminderEmailTemplate(
                            parent.FirstName, parent.LastName, settings);
                        emailService.QueueEmail(parent.Email, subject, body);
                    }

                    var notificationDto = new CreateNotificationDto
                    {
                        UserId = parent.Id,
                        Title = "Nhắc nhở: Còn 5 ngày để đăng ký | Reminder: 5 Days Left to Register",
                        Message = $"Còn 5 ngày nữa là hết hạn đăng ký cho kỳ học {settings.SemesterName} ({settings.AcademicYear}). Vui lòng hoàn tất đăng ký sớm. | Only 5 days left to register for {settings.SemesterName} ({settings.AcademicYear}). Please complete your registration soon.",
                        NotificationType = NotificationType.EnrollmentRegistration,
                        RecipientType = RecipientType.Parent,
                        RelatedEntityId = settings.Id,
                        RelatedEntityType = "EnrollmentSemesterSettings",
                        Metadata = new Dictionary<string, object>
                        {
                            { "semesterCode", settings.SemesterCode },
                            { "semesterName", settings.SemesterName },
                            { "academicYear", settings.AcademicYear },
                            { "registrationStartDate", settings.RegistrationStartDate.ToString("yyyy-MM-dd") },
                            { "registrationEndDate", settings.RegistrationEndDate.ToString("yyyy-MM-dd") },
                            { "notificationKey", notificationKey },
                            { "notificationType", "registration_reminder" }
                        }
                    };

                    await notificationService.CreateNotificationAsync(notificationDto);
                    Interlocked.Increment(ref successCount);
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref errorCount);
                    _logger.LogError(ex, "Error sending registration reminder notification to parent {ParentId}",
                        parent.Id);
                }
            });

            await Task.WhenAll(tasks);
            _logger.LogInformation("Registration reminder notifications - Success: {SuccessCount}, Skipped: {SkippedCount}, Errors: {ErrorCount}",
                successCount, skippedCount, errorCount);
        }

        private async Task SendRegistrationEndNotificationsAsync(
            EnrollmentSemesterSettings settings,
            IUserAccountRepository userAccountRepo,
            IEmailService emailService,
            INotificationService notificationService)
        {
            var notificationKey = $"registration_end_{settings.Id}_{settings.RegistrationEndDate:yyyyMMdd}";
            
            var parents = await userAccountRepo.GetActiveParentUsersAsync();
            
            if (!parents.Any())
            {
                _logger.LogInformation("Registration end: No active parent accounts found");
                return;
            }

            var parentList = parents.ToList();
            int successCount = 0;
            int skippedCount = 0;
            int errorCount = 0;

            var tasks = parentList.Select(async parent =>
            {
                try
                {
                    var existingNotification = await notificationService.GetNotificationByMetadataAsync(
                        parent.Id, "EnrollmentSemesterSettings", notificationKey);

                    if (existingNotification != null)
                    {
                        Interlocked.Increment(ref skippedCount);
                        return;
                    }

                    if (!string.IsNullOrWhiteSpace(parent.Email))
                    {
                        var (subject, body) = CreateRegistrationEndEmailTemplate(
                            parent.FirstName, parent.LastName, settings);
                        emailService.QueueEmail(parent.Email, subject, body);
                    }

                    var notificationDto = new CreateNotificationDto
                    {
                        UserId = parent.Id,
                        Title = "Hết hạn đăng ký cho kỳ học mới | Registration Period Ended",
                        Message = $"Hôm nay là ngày cuối cùng để đăng ký cho kỳ học {settings.SemesterName} ({settings.AcademicYear}). Nếu bạn chưa đăng ký, vui lòng liên hệ với chúng tôi. | Today is the last day to register for {settings.SemesterName} ({settings.AcademicYear}). If you haven't registered yet, please contact us.",
                        NotificationType = NotificationType.EnrollmentRegistration,
                        RecipientType = RecipientType.Parent,
                        RelatedEntityId = settings.Id,
                        RelatedEntityType = "EnrollmentSemesterSettings",
                        Metadata = new Dictionary<string, object>
                        {
                            { "semesterCode", settings.SemesterCode },
                            { "semesterName", settings.SemesterName },
                            { "academicYear", settings.AcademicYear },
                            { "registrationStartDate", settings.RegistrationStartDate.ToString("yyyy-MM-dd") },
                            { "registrationEndDate", settings.RegistrationEndDate.ToString("yyyy-MM-dd") },
                            { "notificationKey", notificationKey },
                            { "notificationType", "registration_end" }
                        }
                    };

                    await notificationService.CreateNotificationAsync(notificationDto);
                    Interlocked.Increment(ref successCount);
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref errorCount);
                    _logger.LogError(ex, "Error sending registration end notification to parent {ParentId}",
                        parent.Id);
                }
            });

            await Task.WhenAll(tasks);
            _logger.LogInformation("Registration end notifications - Success: {SuccessCount}, Skipped: {SkippedCount}, Errors: {ErrorCount}",
                successCount, skippedCount, errorCount);
        }

        private (string subject, string body) CreateRegistrationStartEmailTemplate(
            string firstName, string lastName, EnrollmentSemesterSettings settings)
        {
            var subject = "🎓 Đơn đăng ký cho kỳ học mới đã bắt đầu | New Semester Registration Started";
            
            var startDateStr = settings.RegistrationStartDate.ToString("dd/MM/yyyy");
            var endDateStr = settings.RegistrationEndDate.ToString("dd/MM/yyyy");
            var semesterStartDateStr = settings.SemesterStartDate.ToString("dd/MM/yyyy");
            var semesterEndDateStr = settings.SemesterEndDate.ToString("dd/MM/yyyy");

            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f5f5f5;"">
    <div style=""max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 20px;"">
        <h2 style=""color: #2E7D32; margin-top: 0;"">🎓 Đơn đăng ký cho kỳ học mới đã bắt đầu</h2>
        
        <p>Xin chào <strong>{firstName} {lastName}</strong>,</p>
        
        <p>Chúng tôi rất vui thông báo rằng <strong>đơn đăng ký cho kỳ học mới đã được mở</strong> trên hệ thống <strong>EduBus</strong>.</p>
        
        <div style=""background-color: #E8F5E8; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #2E7D32;"">
            <h3 style=""color: #2E7D32; margin-top: 0;"">📅 Thông tin kỳ học:</h3>
            <p style=""margin: 10px 0;""><strong>Tên kỳ học:</strong> {settings.SemesterName}</p>
            <p style=""margin: 10px 0;""><strong>Năm học:</strong> {settings.AcademicYear}</p>
            <p style=""margin: 10px 0;""><strong>Mã kỳ học:</strong> {settings.SemesterCode}</p>
            <p style=""margin: 10px 0;""><strong>Thời gian kỳ học:</strong> {semesterStartDateStr} - {semesterEndDateStr}</p>
            <p style=""margin: 10px 0;""><strong>Thời gian đăng ký:</strong> {startDateStr} - {endDateStr}</p>
        </div>
        
        <div style=""background-color: #FFF3E0; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #F57C00;"">
            <h3 style=""color: #F57C00; margin-top: 0;"">📝 Hướng dẫn đăng ký:</h3>
            <ol style=""line-height: 1.8;"">
                <li><strong>Bước 1:</strong> Đăng nhập vào ứng dụng EduBus bằng tài khoản của bạn</li>
                <li><strong>Bước 2:</strong> Vào mục ""Register Service"" hoặc ""Register Service""</li>
                <li><strong>Bước 3:</strong> Chọn kỳ học <strong>{settings.SemesterName}</strong> ({settings.AcademicYear})</li>
                <li><strong>Bước 4:</strong> Chọn điểm đón phù hợp cho con của bạn</li>
                <li><strong>Bước 5:</strong> Xem lại thông tin và xác nhận đăng ký</li>
                <li><strong>Bước 6:</strong> Thanh toán phí dịch vụ theo hướng dẫn trong ứng dụng</li>
            </ol>
        </div>
        
        <div style=""background-color: #E3F2FD; padding: 15px; border-radius: 8px; margin: 20px 0;"">
            <p style=""margin: 0; color: #1976D2;""><strong>💡 Lưu ý:</strong> Vui lòng hoàn tất đăng ký trước ngày <strong>{endDateStr}</strong> để đảm bảo chỗ cho con bạn. Số lượng chỗ có hạn.</p>
        </div>
        
        <p>Nếu bạn gặp bất kỳ khó khăn nào trong quá trình đăng ký, vui lòng liên hệ bộ phận hỗ trợ của chúng tôi.</p>
        
        <p style=""margin-top: 30px;"">Trân trọng,<br>
        <strong style=""color: #2E7D32;"">Đội ngũ EduBus</strong></p>
        
        <hr style=""border: none; border-top: 1px solid #e0e0e0; margin: 30px 0;"">
        
        <h2 style=""color: #2E7D32;"">🎓 New Semester Registration Started</h2>
        
        <p>Hello <strong>{firstName} {lastName}</strong>,</p>
        
        <p>We are pleased to inform you that <strong>registration for the new semester has opened</strong> on the <strong>EduBus</strong> system.</p>
        
        <div style=""background-color: #E8F5E8; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #2E7D32;"">
            <h3 style=""color: #2E7D32; margin-top: 0;"">📅 Semester Information:</h3>
            <p style=""margin: 10px 0;""><strong>Semester Name:</strong> {settings.SemesterName}</p>
            <p style=""margin: 10px 0;""><strong>Academic Year:</strong> {settings.AcademicYear}</p>
            <p style=""margin: 10px 0;""><strong>Semester Code:</strong> {settings.SemesterCode}</p>
            <p style=""margin: 10px 0;""><strong>Semester Period:</strong> {semesterStartDateStr} - {semesterEndDateStr}</p>
            <p style=""margin: 10px 0;""><strong>Registration Period:</strong> {startDateStr} - {endDateStr}</p>
        </div>
        
        <div style=""background-color: #FFF3E0; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #F57C00;"">
            <h3 style=""color: #F57C00; margin-top: 0;"">📝 Registration Instructions:</h3>
            <ol style=""line-height: 1.8;"">
                <li><strong>Step 1:</strong> Log in to the EduBus app using your account</li>
                <li><strong>Step 2:</strong> Go to ""Register Service"" section</li>
                <li><strong>Step 3:</strong> Select semester <strong>{settings.SemesterName}</strong> ({settings.AcademicYear})</li>
                <li><strong>Step 4:</strong> Choose a suitable pickup point for your child</li>
                <li><strong>Step 5:</strong> Review the information and confirm your registration</li>
                <li><strong>Step 6:</strong> Make payment for the service fee as instructed in the app</li>
            </ol>
        </div>
        
        <div style=""background-color: #E3F2FD; padding: 15px; border-radius: 8px; margin: 20px 0;"">
            <p style=""margin: 0; color: #1976D2;""><strong>💡 Note:</strong> Please complete your registration before <strong>{endDateStr}</strong> to secure a spot for your child. Limited spots available.</p>
        </div>
        
        <p>If you encounter any difficulties during registration, please contact our support team.</p>
        
        <p style=""margin-top: 30px;"">Best regards,<br>
        <strong style=""color: #2E7D32;"">EduBus Team</strong></p>
    </div>
</body>
</html>";

            return (subject, body);
        }

        private (string subject, string body) CreateRegistrationReminderEmailTemplate(
            string firstName, string lastName, EnrollmentSemesterSettings settings)
        {
            var subject = "⏰ Nhắc nhở: Còn 5 ngày để đăng ký | Reminder: 5 Days Left to Register";
            
            var endDateStr = settings.RegistrationEndDate.ToString("dd/MM/yyyy");
            var semesterStartDateStr = settings.SemesterStartDate.ToString("dd/MM/yyyy");
            var semesterEndDateStr = settings.SemesterEndDate.ToString("dd/MM/yyyy");

            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f5f5f5;"">
    <div style=""max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 20px;"">
        <h2 style=""color: #F57C00; margin-top: 0;"">⏰ Nhắc nhở: Còn 5 ngày để đăng ký</h2>
        
        <p>Xin chào <strong>{firstName} {lastName}</strong>,</p>
        
        <p>Chúng tôi muốn nhắc nhở bạn rằng <strong>còn 5 ngày nữa</strong> là hết hạn đăng ký cho kỳ học mới trên hệ thống <strong>EduBus</strong>.</p>
        
        <div style=""background-color: #FFF3E0; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #F57C00;"">
            <h3 style=""color: #F57C00; margin-top: 0;"">📅 Thông tin kỳ học:</h3>
            <p style=""margin: 10px 0;""><strong>Tên kỳ học:</strong> {settings.SemesterName}</p>
            <p style=""margin: 10px 0;""><strong>Năm học:</strong> {settings.AcademicYear}</p>
            <p style=""margin: 10px 0;""><strong>Thời gian kỳ học:</strong> {semesterStartDateStr} - {semesterEndDateStr}</p>
            <p style=""margin: 10px 0;""><strong>Hạn đăng ký:</strong> <strong style=""color: #D32F2F;"">{endDateStr}</strong></p>
        </div>
        
        <div style=""background-color: #FFEBEE; padding: 15px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #D32F2F;"">
            <p style=""margin: 0; color: #D32F2F;""><strong>⚠️ Quan trọng:</strong> Vui lòng hoàn tất đăng ký trước ngày <strong>{endDateStr}</strong>. Sau ngày này, bạn sẽ không thể đăng ký cho kỳ học này nữa.</p>
        </div>
        
        <div style=""background-color: #E3F2FD; padding: 15px; border-radius: 8px; margin: 20px 0;"">
            <p style=""margin: 0; color: #1976D2;""><strong>💡 Gợi ý:</strong> Nếu bạn chưa đăng ký, vui lòng đăng nhập vào ứng dụng EduBus và hoàn tất đăng ký ngay hôm nay để tránh quên.</p>
        </div>
        
        <p>Nếu bạn đã đăng ký rồi, bạn có thể bỏ qua email này. Nếu bạn gặp bất kỳ khó khăn nào, vui lòng liên hệ bộ phận hỗ trợ của chúng tôi.</p>
        
        <p style=""margin-top: 30px;"">Trân trọng,<br>
        <strong style=""color: #2E7D32;"">Đội ngũ EduBus</strong></p>
        
        <hr style=""border: none; border-top: 1px solid #e0e0e0; margin: 30px 0;"">
        
        <h2 style=""color: #F57C00;"">⏰ Reminder: 5 Days Left to Register</h2>
        
        <p>Hello <strong>{firstName} {lastName}</strong>,</p>
        
        <p>We would like to remind you that there are <strong>only 5 days left</strong> to register for the new semester on the <strong>EduBus</strong> system.</p>
        
        <div style=""background-color: #FFF3E0; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #F57C00;"">
            <h3 style=""color: #F57C00; margin-top: 0;"">📅 Semester Information:</h3>
            <p style=""margin: 10px 0;""><strong>Semester Name:</strong> {settings.SemesterName}</p>
            <p style=""margin: 10px 0;""><strong>Academic Year:</strong> {settings.AcademicYear}</p>
            <p style=""margin: 10px 0;""><strong>Semester Period:</strong> {semesterStartDateStr} - {semesterEndDateStr}</p>
            <p style=""margin: 10px 0;""><strong>Registration Deadline:</strong> <strong style=""color: #D32F2F;"">{endDateStr}</strong></p>
        </div>
        
        <div style=""background-color: #FFEBEE; padding: 15px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #D32F2F;"">
            <p style=""margin: 0; color: #D32F2F;""><strong>⚠️ Important:</strong> Please complete your registration before <strong>{endDateStr}</strong>. After this date, you will not be able to register for this semester.</p>
        </div>
        
        <div style=""background-color: #E3F2FD; padding: 15px; border-radius: 8px; margin: 20px 0;"">
            <p style=""margin: 0; color: #1976D2;""><strong>💡 Tip:</strong> If you haven't registered yet, please log in to the EduBus app and complete your registration today to avoid missing the deadline.</p>
        </div>
        
        <p>If you have already registered, you can ignore this email. If you encounter any difficulties, please contact our support team.</p>
        
        <p style=""margin-top: 30px;"">Best regards,<br>
        <strong style=""color: #2E7D32;"">EduBus Team</strong></p>
    </div>
</body>
</html>";

            return (subject, body);
        }

        private (string subject, string body) CreateRegistrationEndEmailTemplate(
            string firstName, string lastName, EnrollmentSemesterSettings settings)
        {
            var subject = "🔔 Hết hạn đăng ký cho kỳ học mới | Registration Period Ended";
            
            var endDateStr = settings.RegistrationEndDate.ToString("dd/MM/yyyy");
            var semesterStartDateStr = settings.SemesterStartDate.ToString("dd/MM/yyyy");
            var semesterEndDateStr = settings.SemesterEndDate.ToString("dd/MM/yyyy");

            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f5f5f5;"">
    <div style=""max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 20px;"">
        <h2 style=""color: #D32F2F; margin-top: 0;"">🔔 Hết hạn đăng ký cho kỳ học mới</h2>
        
        <p>Xin chào <strong>{firstName} {lastName}</strong>,</p>
        
        <p>Thông báo quan trọng: <strong>Hôm nay ({endDateStr}) là ngày cuối cùng</strong> để đăng ký cho kỳ học mới trên hệ thống <strong>EduBus</strong>.</p>
        
        <div style=""background-color: #FFEBEE; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #D32F2F;"">
            <h3 style=""color: #D32F2F; margin-top: 0;"">📅 Thông tin kỳ học:</h3>
            <p style=""margin: 10px 0;""><strong>Tên kỳ học:</strong> {settings.SemesterName}</p>
            <p style=""margin: 10px 0;""><strong>Năm học:</strong> {settings.AcademicYear}</p>
            <p style=""margin: 10px 0;""><strong>Thời gian kỳ học:</strong> {semesterStartDateStr} - {semesterEndDateStr}</p>
            <p style=""margin: 10px 0;""><strong>Hạn đăng ký:</strong> <strong style=""color: #D32F2F;"">{endDateStr}</strong> (Hôm nay)</p>
        </div>
        
        <div style=""background-color: #FFF3E0; padding: 15px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #F57C00;"">
            <p style=""margin: 0; color: #F57C00;""><strong>⚠️ Lưu ý:</strong> Nếu bạn chưa đăng ký, vui lòng hoàn tất đăng ký <strong>ngay hôm nay</strong> trước khi hệ thống đóng đăng ký. Sau ngày hôm nay, bạn sẽ không thể đăng ký cho kỳ học này nữa.</p>
        </div>
        
        <div style=""background-color: #E3F2FD; padding: 15px; border-radius: 8px; margin: 20px 0;"">
            <p style=""margin: 0; color: #1976D2;""><strong>💡 Thông tin:</strong> Nếu bạn đã đăng ký rồi, bạn có thể bỏ qua email này. Nếu bạn gặp khó khăn hoặc cần hỗ trợ, vui lòng liên hệ bộ phận hỗ trợ của chúng tôi ngay lập tức.</p>
        </div>
        
        <p>Chúng tôi cảm ơn bạn đã sử dụng dịch vụ của EduBus.</p>
        
        <p style=""margin-top: 30px;"">Trân trọng,<br>
        <strong style=""color: #2E7D32;"">Đội ngũ EduBus</strong></p>
        
        <hr style=""border: none; border-top: 1px solid #e0e0e0; margin: 30px 0;"">
        
        <h2 style=""color: #D32F2F;"">🔔 Registration Period Ended</h2>
        
        <p>Hello <strong>{firstName} {lastName}</strong>,</p>
        
        <p>Important notice: <strong>Today ({endDateStr}) is the last day</strong> to register for the new semester on the <strong>EduBus</strong> system.</p>
        
        <div style=""background-color: #FFEBEE; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #D32F2F;"">
            <h3 style=""color: #D32F2F; margin-top: 0;"">📅 Semester Information:</h3>
            <p style=""margin: 10px 0;""><strong>Semester Name:</strong> {settings.SemesterName}</p>
            <p style=""margin: 10px 0;""><strong>Academic Year:</strong> {settings.AcademicYear}</p>
            <p style=""margin: 10px 0;""><strong>Semester Period:</strong> {semesterStartDateStr} - {semesterEndDateStr}</p>
            <p style=""margin: 10px 0;""><strong>Registration Deadline:</strong> <strong style=""color: #D32F2F;"">{endDateStr}</strong> (Today)</p>
        </div>
        
        <div style=""background-color: #FFF3E0; padding: 15px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #F57C00;"">
            <p style=""margin: 0; color: #F57C00;""><strong>⚠️ Note:</strong> If you haven't registered yet, please complete your registration <strong>today</strong> before the system closes registration. After today, you will not be able to register for this semester.</p>
        </div>
        
        <div style=""background-color: #E3F2FD; padding: 15px; border-radius: 8px; margin: 20px 0;"">
            <p style=""margin: 0; color: #1976D2;""><strong>💡 Information:</strong> If you have already registered, you can ignore this email. If you encounter difficulties or need assistance, please contact our support team immediately.</p>
        </div>
        
        <p>Thank you for using EduBus services.</p>
        
        <p style=""margin-top: 30px;"">Best regards,<br>
        <strong style=""color: #2E7D32;"">EduBus Team</strong></p>
    </div>
</body>
</html>";

            return (subject, body);
        }
    }
}

