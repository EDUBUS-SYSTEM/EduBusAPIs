using Data.Models;
using Data.Repos.Interfaces;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Services.Contracts;
using Services.Models.PickupPoint;

namespace Services.Implementations
{
    /// Orchestrates the end-to-end flow for Parent pickup point registration:
    /// - Check parent email
    /// - Send/verify OTP (via Redis store)
    /// - Create request to Mongo
    /// - Admin approve -> create PickupPoint + Student history in SQL
    /// </summary>
    public class PickupPointEnrollmentService : IPickupPointEnrollmentService
    {
        private readonly IStudentRepository _studentRepo;
        private readonly IPickupPointRepository _pickupPointRepo;
        private readonly IStudentPickupPointHistoryRepository _historyRepo;
        private readonly IPickupPointRequestRepository _requestRepo;
        private readonly IParentRegistrationRepository _parentRegistrationRepo;
        private readonly IEmailService _email;
        private readonly DbContext _sqlDb;
        private readonly IOtpStore _otpStore;
        private readonly IParentService _parentService;

        private const string Purpose = "PickupPointRequest";
        private const decimal MaxEstimatedPrice = 50_000_000m; // sanity cap

        public PickupPointEnrollmentService(
            IStudentRepository studentRepo,
            IPickupPointRepository pickupPointRepo,
            IStudentPickupPointHistoryRepository historyRepo,
            IPickupPointRequestRepository requestRepo,
            IParentRegistrationRepository parentRegistrationRepo,
            IEmailService email,
            DbContext sqlDb,
            IOtpStore otpStore,
            IParentService parentService)
        {
            _studentRepo = studentRepo;
            _pickupPointRepo = pickupPointRepo;
            _historyRepo = historyRepo;
            _requestRepo = requestRepo;
            _parentRegistrationRepo = parentRegistrationRepo;
            _email = email;
            _sqlDb = sqlDb;
            _otpStore = otpStore;
            _parentService = parentService;
        }

        public async Task<ParentRegistrationResponseDto> RegisterParentAsync(ParentRegistrationRequestDto dto)
        {
            // Check if email already exists in system
            var emailExists = await CheckParentEmailExistsAsync(dto.Email);
            
            // Check if there's already a pending registration for this email
            var existingRegistration = await _parentRegistrationRepo.FindByEmailAsync(dto.Email);
            if (existingRegistration != null)
            {
                // Update existing registration with new data
                existingRegistration.FirstName = dto.FirstName;
                existingRegistration.LastName = dto.LastName;
                existingRegistration.PhoneNumber = dto.PhoneNumber;
                existingRegistration.Address = dto.Address;
                existingRegistration.DateOfBirth = dto.DateOfBirth;
                existingRegistration.Gender = (int)dto.Gender;
                existingRegistration.ExpiresAt = DateTime.UtcNow.AddHours(24);
                
                await _parentRegistrationRepo.UpdateAsync(existingRegistration);
            }
            else
            {
                // Create new registration document
                var registration = new ParentRegistrationDocument
                {
                    Email = dto.Email,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    PhoneNumber = dto.PhoneNumber,
                    Address = dto.Address,
                    DateOfBirth = dto.DateOfBirth,
                    Gender = (int)dto.Gender,
                    Status = "Pending",
                    ExpiresAt = DateTime.UtcNow.AddHours(24)
                };
                
                await _parentRegistrationRepo.AddAsync(registration);
            }

            // Send OTP
            var otp = GenerateOtp(6);
            var hash = BCrypt.Net.BCrypt.HashPassword(otp);

            // Save OTP to Redis with TTL and single-use constraint
            var saved = await _otpStore.SaveAsync(Purpose, dto.Email, hash, TimeSpan.FromMinutes(5), overwrite: false);
            if (!saved)
                throw new InvalidOperationException("An OTP is still valid. Please check your email or try again later.");

            // Send OTP email
            var subject = "[EduBus] Mã xác thực OTP";
            var body = $@"
<p>Xin chào,</p>
<p>Mã OTP của bạn là <b>{otp}</b>, có hiệu lực trong <b>5 phút</b>.</p>
<p>Vui lòng sử dụng mã này để xác thực đăng ký dịch vụ đưa đón học sinh.</p>
<p>Nếu bạn không yêu cầu thao tác này, vui lòng bỏ qua email.</p>
<p>Trân trọng,<br/>EduBus Team</p>";

            await _email.SendEmailAsync(dto.Email, subject, body);

            return new ParentRegistrationResponseDto
            {
                RegistrationId = existingRegistration?.Id ?? Guid.NewGuid(),
                Email = dto.Email,
                EmailExists = emailExists,
                OtpSent = true,
                Message = "Registration information saved. OTP has been sent to your email for verification."
            };
        }

        /// <summary>
        /// Check if there exists at least one student bound to the given parent email.
        /// </summary>
        private async Task<bool> CheckParentEmailExistsAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            return await _studentRepo.GetQueryable()
                .AnyAsync(s => s.ParentEmail == email && !s.IsDeleted);
        }

        public async Task<VerifyOtpWithStudentsResponseDto> VerifyOtpWithStudentsAsync(string email, string otp)
        {
            var (hash, attempts, max) = await _otpStore.GetAsync(Purpose, email);
            if (hash is null)
            {
                return new VerifyOtpWithStudentsResponseDto
                {
                    Verified = false,
                    Message = "OTP không tồn tại hoặc đã hết hạn.",
                    Students = new List<StudentBriefDto>(),
                    EmailExists = false
                };
            }

            if (attempts >= max)
            {
                await _otpStore.DeleteAsync(Purpose, email);
                return new VerifyOtpWithStudentsResponseDto
                {
                    Verified = false,
                    Message = "OTP đã hết số lần thử. Vui lòng yêu cầu mã mới.",
                    Students = new List<StudentBriefDto>(),
                    EmailExists = false
                };
            }

            var ok = BCrypt.Net.BCrypt.Verify(otp, hash);
            if (!ok)
            {
                var (allowed, _, _) = await _otpStore.IncrementAttemptsAsync(Purpose, email);
                if (!allowed) await _otpStore.DeleteAsync(Purpose, email);
                
                return new VerifyOtpWithStudentsResponseDto
                {
                    Verified = false,
                    Message = "Mã OTP không đúng. Vui lòng kiểm tra lại.",
                    Students = new List<StudentBriefDto>(),
                    EmailExists = false
                };
            }

            // OTP verified successfully
            await _otpStore.DeleteAsync(Purpose, email);
            
            // Check if email exists in system and get students
            var emailExists = await CheckParentEmailExistsAsync(email);
            var students = emailExists ? await GetStudentsByEmailAsync(email) : new List<StudentBriefDto>();

            return new VerifyOtpWithStudentsResponseDto
            {
                Verified = true,
                Message = "Xác thực OTP thành công.",
                Students = students,
                EmailExists = emailExists
            };
        }
        public async Task<List<StudentBriefDto>> GetStudentsByEmailAsync(string email)
        {
            var students = await _studentRepo.GetStudentsByParentEmailAsync(email);
            return students
                .Select(s => new StudentBriefDto { Id = s.Id, FirstName = s.FirstName, LastName = s.LastName })
                .ToList();
        }


        public async Task<SubmitPickupPointRequestResponseDto> SubmitPickupPointRequestAsync(SubmitPickupPointRequestDto dto)
        {
            // Check if email exists in system (for students validation)
            var emailExists = await CheckParentEmailExistsAsync(dto.Email);
            
            // Validate students if email exists
            if (emailExists)
            {
                if (dto.StudentIds is null || dto.StudentIds.Count == 0)
                    throw new ArgumentException("At least one student must be selected.", nameof(dto.StudentIds));

                var validStudents = await _studentRepo.GetQueryable()
                    .Where(s => s.ParentEmail == dto.Email && dto.StudentIds.Contains(s.Id) && !s.IsDeleted)
                    .Select(s => s.Id)
                    .ToListAsync();

                if (validStudents.Count != dto.StudentIds.Count)
                    throw new InvalidOperationException("The student list contains one or more invalid items.");
            }

            if (dto.EstimatedPriceVnd <= 0 || dto.EstimatedPriceVnd > MaxEstimatedPrice)
                throw new ArgumentOutOfRangeException(nameof(dto.EstimatedPriceVnd), "Estimated price is out of allowed range.");

            var doc = new PickupPointRequestDocument
            {
                ParentEmail = dto.Email,
                StudentIds = dto.StudentIds,
                AddressText = dto.AddressText?.Trim() ?? "",
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                DistanceKm = Math.Round(dto.DistanceKm, 3),
                Description = dto.Description?.Trim() ?? "",
                Reason = dto.Reason?.Trim() ?? "",
                Status = "Pending",
                UnitPriceVndPerKm = dto.UnitPriceVndPerKm,
                EstimatedPriceVnd = dto.EstimatedPriceVnd,
                CreatedAt = DateTime.UtcNow
            };

            var createdDoc = await _requestRepo.AddAsync(doc);

            return new SubmitPickupPointRequestResponseDto
            {
                RequestId = createdDoc.Id,
                Status = "Pending",
                Message = "Pickup point request submitted successfully. Please wait for admin approval.",
                EstimatedPriceVnd = dto.EstimatedPriceVnd,
                CreatedAt = createdDoc.CreatedAt
            };
        }


        public async Task<List<PickupPointRequestDetailDto>> ListRequestDetailsAsync(PickupPointRequestListQuery query)
        {
            var requests = await _requestRepo.QueryAsync(query.Status, query.ParentEmail, query.Skip, query.Take);
            var result = new List<PickupPointRequestDetailDto>();

            foreach (var request in requests)
            {
                var detail = new PickupPointRequestDetailDto
                {
                    Id = request.Id,
                    ParentEmail = request.ParentEmail,
                    AddressText = request.AddressText,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    DistanceKm = request.DistanceKm,
                    Description = request.Description,
                    Reason = request.Reason,
                    UnitPriceVndPerKm = request.UnitPriceVndPerKm,
                    EstimatedPriceVnd = request.EstimatedPriceVnd,
                    Status = request.Status,
                    AdminNotes = request.AdminNotes,
                    ReviewedAt = request.ReviewedAt,
                    ReviewedByAdminId = request.ReviewedByAdminId,
                    CreatedAt = request.CreatedAt,
                    UpdatedAt = request.UpdatedAt
                };

                // Get parent registration information
                var parentRegistration = await _parentRegistrationRepo.FindByEmailAsync(request.ParentEmail);
                if (parentRegistration != null)
                {
                    detail.ParentInfo = new ParentRegistrationInfoDto
                    {
                        FirstName = parentRegistration.FirstName,
                        LastName = parentRegistration.LastName,
                        PhoneNumber = parentRegistration.PhoneNumber,
                        Address = parentRegistration.Address,
                        DateOfBirth = parentRegistration.DateOfBirth,
                        Gender = parentRegistration.Gender,
                        CreatedAt = parentRegistration.CreatedAt
                    };
                }

                // Get students information
                if (request.StudentIds.Any())
                {
                    var students = await _studentRepo.GetQueryable()
                        .Where(s => request.StudentIds.Contains(s.Id) && !s.IsDeleted)
                        .Select(s => new StudentBriefDto 
                        { 
                            Id = s.Id, 
                            FirstName = s.FirstName, 
                            LastName = s.LastName 
                        })
                        .ToListAsync();
                    detail.Students = students;
                }

                result.Add(detail);
            }

            return result;
        }

        public async Task ApproveRequestAsync(Guid requestId, Guid adminId, string? notes)
        {
            var req = await _requestRepo.FindAsync(requestId)
                      ?? throw new KeyNotFoundException("Request not found.");

            if (req.Status == "Rejected")
                throw new InvalidOperationException("This request has been rejected and cannot be approved.");

            if (req.Status == "Approved") return; // idempotent

            var strategy = _sqlDb.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _sqlDb.Database.BeginTransactionAsync();
                try
                {
                    var pp = new PickupPoint
                    {
                        Description = string.IsNullOrWhiteSpace(req.Description) ? "Pickup Point" : req.Description,
                        Location = req.AddressText,
                        Geog = new Point(req.Longitude, req.Latitude) { SRID = 4326 }
                    };
                    await _pickupPointRepo.AddAsync(pp);

                    var now = DateTime.UtcNow;

                    foreach (var sid in req.StudentIds)
                    {
                        var s = await _studentRepo.FindAsync(sid);
                        if (s == null || s.IsDeleted) continue;

                        // Close previous assignment if exists
                        if (s.CurrentPickupPointId.HasValue)
                        {
                            await _historyRepo.AddAsync(new StudentPickupPointHistory
                            {
                                StudentId = s.Id,
                                PickupPointId = s.CurrentPickupPointId.Value,
                                AssignedAt = s.PickupPointAssignedAt ?? now.AddMinutes(-1),
                                RemovedAt = now,
                                ChangeReason = "Reassigned by approval",
                                ChangedBy = $"Admin:{adminId}"
                            });
                        }

                        // Assign new pickup point
                        s.CurrentPickupPointId = pp.Id;
                        s.PickupPointAssignedAt = now;
                        await _studentRepo.UpdateAsync(s);

                        // Write assignment history
                        await _historyRepo.AddAsync(new StudentPickupPointHistory
                        {
                            StudentId = s.Id,
                            PickupPointId = pp.Id,
                            AssignedAt = now,
                            ChangeReason = "Approved pickup point request",
                            ChangedBy = $"Admin:{adminId}"
                        });
                    }

                    await tx.CommitAsync();
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            });

            // Create user account for parent if not exists
            var parentRegistration = await _parentRegistrationRepo.FindByEmailAsync(req.ParentEmail);
            if (parentRegistration != null)
            {
                try
                {
                    // Check if parent already exists in system
                    var emailExists = await CheckParentEmailExistsAsync(req.ParentEmail);
                    if (!emailExists)
                    {
                        // Create parent account
                        var createParentRequest = new Services.Models.Parent.CreateParentRequest
                        {
                            Email = parentRegistration.Email,
                            FirstName = parentRegistration.FirstName,
                            LastName = parentRegistration.LastName,
                            PhoneNumber = parentRegistration.PhoneNumber,
                            Address = parentRegistration.Address,
                            DateOfBirth = parentRegistration.DateOfBirth,
                            Gender = (Data.Models.Enums.Gender)parentRegistration.Gender
                        };

                        var createUserResponse = await _parentService.CreateParentAsync(createParentRequest);
                        
                        // Send approval email with account details
                        await SendApprovalEmailWithAccountAsync(
                            req.ParentEmail,
                            parentRegistration.FirstName,
                            parentRegistration.LastName,
                            createUserResponse.Password,
                            req.AddressText,
                            req.EstimatedPriceVnd);
                    }
                    else
                    {
                        // Parent already exists, just send approval email
                        await SendApprovalEmailAsync(
                            req.ParentEmail,
                            parentRegistration.FirstName,
                            parentRegistration.LastName,
                            req.AddressText,
                            req.EstimatedPriceVnd);
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't fail the approval process
                    // The pickup point is already created and assigned
                    // TODO: Add proper logging here
                    Console.WriteLine($"Error creating parent account for {req.ParentEmail}: {ex.Message}");
                    
                    // Send basic approval email without account details
                    await SendApprovalEmailAsync(
                        req.ParentEmail,
                        parentRegistration.FirstName,
                        parentRegistration.LastName,
                        req.AddressText,
                        req.EstimatedPriceVnd);
                }
            }

            // Update Mongo request status (outside SQL retry scope)
            req.Status = "Approved";
            req.ReviewedAt = DateTime.UtcNow;
            req.ReviewedByAdminId = adminId;
            req.AdminNotes = notes ?? "";
            await _requestRepo.UpdateAsync(req);
        }
        public async Task RejectRequestAsync(Guid requestId, Guid adminId, string reason)
        {
            var req = await _requestRepo.FindAsync(requestId)
                      ?? throw new KeyNotFoundException("Request not found.");

            if (req.Status == "Approved")
                throw new InvalidOperationException("This request has already been approved.");

            req.Status = "Rejected";
            req.ReviewedAt = DateTime.UtcNow;
            req.ReviewedByAdminId = adminId;
            req.AdminNotes = reason?.Trim() ?? "";
            await _requestRepo.UpdateAsync(req);

            // Send rejection email to parent
            var parentRegistration = await _parentRegistrationRepo.FindByEmailAsync(req.ParentEmail);
            if (parentRegistration != null)
            {
                await SendRejectionEmailAsync(
                    req.ParentEmail,
                    parentRegistration.FirstName,
                    parentRegistration.LastName,
                    reason?.Trim() ?? "Không đủ điều kiện",
                    req.AddressText);
            }
        }

        private static string GenerateOtp(int len)
        {
            var rnd = Random.Shared;
            return string.Concat(Enumerable.Range(0, len).Select(_ => rnd.Next(0, 10)));
        }
        
        private async Task SendApprovalEmailWithAccountAsync(
            string email, 
            string firstName, 
            string lastName, 
            string password, 
            string pickupAddress, 
            decimal estimatedPrice)
        {
            var subject = "🎉 Đơn đăng ký điểm đón được phê duyệt - Tài khoản đã được tạo";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #2E7D32;'>🎉 Chúc mừng! Đơn đăng ký của bạn đã được phê duyệt</h2>
                    
                    <p>Xin chào <strong>{firstName} {lastName}</strong>,</p>
                    
                    <p>Chúng tôi rất vui mừng thông báo rằng đơn đăng ký sử dụng dịch vụ đưa đón của bạn đã được phê duyệt thành công!</p>
                    
                    <div style='background-color: #E8F5E8; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <h3 style='color: #2E7D32; margin-top: 0;'>📋 Thông tin tài khoản của bạn:</h3>
                        <p><strong>Email đăng nhập:</strong> {email}</p>
                        <p><strong>Mật khẩu:</strong> <code style='background-color: #f5f5f5; padding: 2px 6px; border-radius: 4px;'>{password}</code></p>
                        <p style='color: #D32F2F; font-size: 14px;'><strong>⚠️ Lưu ý:</strong> Vui lòng đổi mật khẩu sau khi đăng nhập lần đầu để bảo mật tài khoản.</p>
                    </div>
                    
                    <div style='background-color: #E3F2FD; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <h3 style='color: #1976D2; margin-top: 0;'>📍 Thông tin điểm đón:</h3>
                        <p><strong>Địa chỉ:</strong> {pickupAddress}</p>
                        <p><strong>Chi phí ước tính:</strong> {estimatedPrice:N0} VNĐ</p>
                    </div>
                    
                    <div style='background-color: #FFF3E0; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <h3 style='color: #F57C00; margin-top: 0;'>🚌 Bước tiếp theo:</h3>
                        <ol>
                            <li>Đăng nhập vào hệ thống bằng tài khoản vừa được tạo</li>
                            <li>Đổi mật khẩu để bảo mật tài khoản</li>
                            <li>Theo dõi lịch trình đưa đón của con em</li>
                            <li>Liên hệ với chúng tôi nếu có bất kỳ thắc mắc nào</li>
                        </ol>
                    </div>
                    
                    <p>Cảm ơn bạn đã tin tưởng sử dụng dịch vụ của chúng tôi!</p>
                    
                    <p>Trân trọng,<br>
                    <strong>Đội ngũ EduBus</strong></p>
                </div>";

            await _email.SendEmailAsync(email, subject, body);
        }
        private async Task SendApprovalEmailAsync(
            string email, 
            string firstName, 
            string lastName, 
            string pickupAddress, 
            decimal estimatedPrice)
        {
            var subject = "🎉 Đơn đăng ký điểm đón được phê duyệt";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #2E7D32;'>🎉 Chúc mừng! Đơn đăng ký của bạn đã được phê duyệt</h2>
                    
                    <p>Xin chào <strong>{firstName} {lastName}</strong>,</p>
                    
                    <p>Chúng tôi rất vui mừng thông báo rằng đơn đăng ký sử dụng dịch vụ đưa đón của bạn đã được phê duyệt thành công!</p>
                    
                    <div style='background-color: #E3F2FD; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <h3 style='color: #1976D2; margin-top: 0;'>📍 Thông tin điểm đón:</h3>
                        <p><strong>Địa chỉ:</strong> {pickupAddress}</p>
                        <p><strong>Chi phí ước tính:</strong> {estimatedPrice:N0} VNĐ</p>
                    </div>
                    
                    <div style='background-color: #FFF3E0; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <h3 style='color: #F57C00; margin-top: 0;'>🚌 Bước tiếp theo:</h3>
                        <ol>
                            <li>Đăng nhập vào hệ thống bằng tài khoản hiện tại</li>
                            <li>Theo dõi lịch trình đưa đón của con em</li>
                            <li>Liên hệ với chúng tôi nếu có bất kỳ thắc mắc nào</li>
                        </ol>
                    </div>
                    
                    <p>Cảm ơn bạn đã tin tưởng sử dụng dịch vụ của chúng tôi!</p>
                    
                    <p>Trân trọng,<br>
                    <strong>Đội ngũ EduBus</strong></p>
                </div>";

            await _email.SendEmailAsync(email, subject, body);
        }

        private async Task SendRejectionEmailAsync(
            string email, 
            string firstName, 
            string lastName, 
            string reason, 
            string pickupAddress)
        {
            var subject = "❌ Thông báo về đơn đăng ký điểm đón";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #D32F2F;'>❌ Thông báo về đơn đăng ký điểm đón</h2>
                    
                    <p>Xin chào <strong>{firstName} {lastName}</strong>,</p>
                    
                    <p>Chúng tôi rất tiếc phải thông báo rằng đơn đăng ký sử dụng dịch vụ đưa đón của bạn chưa thể được phê duyệt tại thời điểm này.</p>
                    
                    <div style='background-color: #FFEBEE; padding: 15px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #D32F2F;'>
                        <h3 style='color: #D32F2F; margin-top: 0;'>📋 Thông tin đơn đăng ký:</h3>
                        <p><strong>Địa chỉ điểm đón:</strong> {pickupAddress}</p>
                        <p><strong>Lý do từ chối:</strong> {reason}</p>
                    </div>
                    
                    <div style='background-color: #E3F2FD; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <h3 style='color: #1976D2; margin-top: 0;'>💡 Gợi ý:</h3>
                        <ul>
                            <li>Vui lòng kiểm tra lại thông tin đăng ký</li>
                            <li>Liên hệ với chúng tôi để được tư vấn thêm</li>
                            <li>Bạn có thể đăng ký lại sau khi khắc phục các vấn đề được nêu</li>
                        </ul>
                    </div>
                    
                    <div style='background-color: #FFF3E0; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <h3 style='color: #F57C00; margin-top: 0;'>📞 Liên hệ hỗ trợ:</h3>
                        <p>Nếu bạn có bất kỳ thắc mắc nào, vui lòng liên hệ với chúng tôi:</p>
                        <ul>
                            <li>Email: support@edubus.com</li>
                            <li>Hotline: 1900-xxxx</li>
                            <li>Thời gian: 8:00 - 17:00 (Thứ 2 - Thứ 6)</li>
                        </ul>
                    </div>
                    
                    <p>Cảm ơn bạn đã quan tâm đến dịch vụ của chúng tôi!</p>
                    
                    <p>Trân trọng,<br>
                    <strong>Đội ngũ EduBus</strong></p>
                </div>";

            await _email.SendEmailAsync(email, subject, body);
        }
    }
}
