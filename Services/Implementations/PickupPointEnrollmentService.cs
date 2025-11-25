using Data.Models;
using Data.Models.Enums;
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
        private readonly IStudentPickupPointRepository _historyRepo;
        private readonly IPickupPointRequestRepository _requestRepo;
        private readonly IParentRegistrationRepository _parentRegistrationRepo;
        private readonly IEmailService _email;
        private readonly DbContext _sqlDb;
        private readonly IOtpStore _otpStore;
        private readonly IParentService _parentService;
        private readonly ITransactionService _transactionService;
        private readonly ITransactionRepository _transactionRepo;
        private readonly ITransportFeeItemRepository _transportFeeItemRepo;

        private const string Purpose = "PickupPointRequest";
        private const decimal MaxEstimatedPrice = 50_000_000m; // sanity cap

        public PickupPointEnrollmentService(
            IStudentRepository studentRepo,
            IPickupPointRepository pickupPointRepo,
            IStudentPickupPointRepository historyRepo,
            IPickupPointRequestRepository requestRepo,
            IParentRegistrationRepository parentRegistrationRepo,
            IEmailService email,
            DbContext sqlDb,
            IOtpStore otpStore,
            IParentService parentService,
            ITransactionService transactionService,
            ITransactionRepository transactionRepo,
            ITransportFeeItemRepository transportFeeItemRepo)
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
            _transactionService = transactionService;
            _transactionRepo = transactionRepo;
            _transportFeeItemRepo = transportFeeItemRepo;
        }

        public async Task<ParentRegistrationResponseDto> RegisterParentAsync(ParentRegistrationRequestDto dto)
        {
            // Check if email already exists in system
            var emailExists = await CheckParentEmailExistsAsync(dto.Email);
            // If the email does not belong to any student, do not proceed
            if (!emailExists)
            {
                return new ParentRegistrationResponseDto
                {
                    RegistrationId = Guid.Empty,
                    Email = dto.Email,
                    EmailExists = false,
                    OtpSent = false,
                    Message = "The provided email is not associated with any student in the system. Please verify or contact the school."
                };
            }
            
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
            var subject = "[EduBus] Your OTP Code";
            var body = $@"
<p>Hello,</p>
<p>Your OTP code is <b>{otp}</b>. It is valid for <b>5 minutes</b>.</p>
<p>Please use this code to verify your registration for the student pickup service.</p>
<p>If you did not request this, please ignore this email.</p>
<p>Best regards,<br/>EduBus Team</p>";

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

            // Check if email exists in Student table (for validation)
            return await _studentRepo.GetQueryable()
                .AnyAsync(s => s.ParentEmail == email && !s.IsDeleted);
        }

        private async Task<bool> CheckParentAccountExistsAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            // Check if parent exists in UserAccounts table (has account)
            return await _sqlDb.Set<Data.Models.UserAccount>()
                .AnyAsync(ua => ua.Email == email && !ua.IsDeleted);
        }

        public async Task<VerifyOtpWithStudentsResponseDto> VerifyOtpWithStudentsAsync(string email, string otp)
        {
            var (hash, attempts, max) = await _otpStore.GetAsync(Purpose, email);
            if (hash is null)
            {
                return new VerifyOtpWithStudentsResponseDto
                {
                    Verified = false,
                    Message = "OTP does not exist or has expired.",
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
                    Message = "OTP attempt limit reached. Please request a new code.",
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
                    Message = "Incorrect OTP. Please try again.",
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
                Message = "OTP verified successfully.",
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

            // Calculate transport fee using TransactionService
            var calculateFeeRequest = new Services.Models.Transaction.CalculateFeeRequest
            {
                DistanceKm = dto.DistanceKm
            };
            
            var feeCalculation = await _transactionService.CalculateTransportFeeAsync(calculateFeeRequest);
            
            // Get next semester information
            var semesterInfo = await _transactionService.GetNextSemesterAsync();

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
                UnitPricePerKm = feeCalculation.UnitPricePerKm,
                TotalFee = feeCalculation.TotalFee,
                SemesterName = semesterInfo.Name,
                SemesterCode = semesterInfo.Code,
                AcademicYear = semesterInfo.AcademicYear,
                SemesterStartDate = semesterInfo.StartDate,
                SemesterEndDate = semesterInfo.EndDate,
                TotalSchoolDays = feeCalculation.TotalSchoolDays,
                CreatedAt = DateTime.UtcNow
            };

            var createdDoc = await _requestRepo.AddAsync(doc);

            return new SubmitPickupPointRequestResponseDto
            {
                RequestId = createdDoc.Id,
                Status = "Pending",
                Message = "Pickup point request submitted successfully. Please wait for admin approval.",
                TotalFee = createdDoc.TotalFee,
                CreatedAt = createdDoc.CreatedAt,
                SemesterName = semesterInfo.Name,
                AcademicYear = semesterInfo.AcademicYear,
                TotalSchoolDays = feeCalculation.TotalSchoolDays,
                CalculationDetails = feeCalculation.CalculationDetails
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
                    UnitPricePerKm = request.UnitPricePerKm,
                    TotalFee = request.TotalFee,
                    // Semester information
                    SemesterName = request.SemesterName,
                    SemesterCode = request.SemesterCode,
                    AcademicYear = request.AcademicYear,
                    SemesterStartDate = request.SemesterStartDate,
                    SemesterEndDate = request.SemesterEndDate,
                    TotalSchoolDays = request.TotalSchoolDays,
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

            // Create pickup point from request (but don't assign to students yet)
            var pickupPoint = new PickupPoint
            {
                Description = string.IsNullOrWhiteSpace(req.Description) ? "Pickup Point" : req.Description,
                Location = req.AddressText,
                Geog = new Point(req.Longitude, req.Latitude) { SRID = 4326 }
            };
            await _pickupPointRepo.AddAsync(pickupPoint);

            // Update request with pickup point ID
            req.PickupPointId = pickupPoint.Id;
            await _requestRepo.UpdateAsync(req);

            // Create StudentPickupPoint records for each student with semester information
            // This replaces the previous logic of assigning pickup point directly to students
            var now = DateTime.UtcNow;
            foreach (var sid in req.StudentIds)
            {
                var s = await _studentRepo.FindAsync(sid);
                if (s == null || s.IsDeleted) continue;

                // Update student status to Pending
                if (s.Status == StudentStatus.Available)
                {
                    s.Status = StudentStatus.Pending;
                    await _studentRepo.UpdateAsync(s);
                }

                // Create StudentPickupPoint record with semester information
                await _historyRepo.AddAsync(new StudentPickupPoint
                {
                    StudentId = sid,
                    PickupPointId = pickupPoint.Id,
                    AssignedAt = now,
                    ChangeReason = "Assigned during approval",
                    ChangedBy = $"Admin:{adminId}",
                    // Semester information from request
                    SemesterName = req.SemesterName,
                    SemesterCode = req.SemesterCode,
                    AcademicYear = req.AcademicYear,
                    SemesterStartDate = req.SemesterStartDate,
                    SemesterEndDate = req.SemesterEndDate,
                    TotalSchoolDays = req.TotalSchoolDays
                });
            }

            // Create user account for parent if not exists
            var parentRegistration = await _parentRegistrationRepo.FindByEmailAsync(req.ParentEmail);
            
            if (parentRegistration != null)
            {
                try
                {
                    // Check if parent already has account in system
                    var accountExists = await CheckParentAccountExistsAsync(req.ParentEmail);
                    
                    if (!accountExists)
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
                            req.TotalFee);
                    }
                    else
                    {
                        // Parent already exists, just send approval email
                        await SendApprovalEmailAsync(
                            req.ParentEmail,
                            parentRegistration.FirstName,
                            parentRegistration.LastName,
                            req.AddressText,
                            req.TotalFee);
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't fail the approval process
                    // Send basic approval email without account details
                    await SendApprovalEmailAsync(
                        req.ParentEmail,
                        parentRegistration.FirstName,
                        parentRegistration.LastName,
                        req.AddressText,
                        req.TotalFee);
                }
            }

            // Create transaction for transport fee
            try
            {
                // Get parent ID from UserAccount table (after account creation)
                var parentAccount = await _sqlDb.Set<Data.Models.UserAccount>()
                    .FirstOrDefaultAsync(ua => ua.Email == req.ParentEmail && !ua.IsDeleted);
                
                if (parentAccount != null)
                {
                    // Get current active unit price
                    var unitPrice = await _transactionService.GetCurrentActiveUnitPriceAsync(null);
                    
                    var createTransactionRequest = new Services.Models.Transaction.CreateTransactionFromPickupPointRequest
                    {
                        PickupPointRequestId = req.Id,
                        ParentId = parentAccount.Id,
                        ParentEmail = req.ParentEmail,
                        StudentIds = req.StudentIds,
                        DistanceKm = req.DistanceKm,
                        UnitPricePerKm = unitPrice.PricePerKm,
                        TotalFee = req.TotalFee, // Use the pre-calculated fee from PickupPointRequest
                        UnitPriceId = unitPrice.Id,
                        ApprovedByAdminId = adminId,
                        AdminNotes = notes ?? ""
                    };

                    var transactionResult = await _transactionService.CreateTransactionFromPickupPointAsync(createTransactionRequest);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the approval process
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
                    reason?.Trim() ?? "Insufficient conditions",
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
            var subject = "🎉 Pickup point request approved - Your account has been created";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #2E7D32;'>🎉 Congratulations! Your request has been approved</h2>
                    
                    <p>Hello <strong>{firstName} {lastName}</strong>,</p>
                    
                    <p>We are pleased to inform you that your request to use the student pickup service has been approved.</p>
                    
                    <div style='background-color: #E8F5E8; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <h3 style='color: #2E7D32; margin-top: 0;'>📋 Your account details:</h3>
                        <p><strong>Login email:</strong> {email}</p>
                        <p><strong>Password:</strong> <code style='background-color: #f5f5f5; padding: 2px 6px; border-radius: 4px;'>{password}</code></p>
                        <p style='color: #D32F2F; font-size: 14px;'><strong>⚠️ Note:</strong> Please change your password after your first login to keep your account secure.</p>
                    </div>
                    
                    <div style='background-color: #E3F2FD; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <h3 style='color: #1976D2; margin-top: 0;'>📍 Pickup point information:</h3>
                        <p><strong>Address:</strong> {pickupAddress}</p>
                        <p><strong>Estimated fee:</strong> {estimatedPrice:N0} VND</p>
                    </div>
                    
                    <div style='background-color: #FFF3E0; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <h3 style='color: #F57C00; margin-top: 0;'>🚌 Next steps:</h3>
                        <ol>
                            <li>Log in using the account above</li>
                            <li>Change your password</li>
                            <li>Track bus schedules for your child</li>
                            <li>Contact us if you need any assistance</li>
                        </ol>
                    </div>
                    
                    <p>Thank you for choosing our service!</p>
                    
                    <p>Best regards,<br>
                    <strong>EduBus Team</strong></p>
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
            var subject = "🎉 Pickup point request approved";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #2E7D32;'>🎉 Congratulations! Your request has been approved</h2>
                    
                    <p>Hello <strong>{firstName} {lastName}</strong>,</p>
                    
                    <p>Your request to use the student pickup service has been approved.</p>
                    
                    <div style='background-color: #E3F2FD; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <h3 style='color: #1976D2; margin-top: 0;'>📍 Pickup point information:</h3>
                        <p><strong>Address:</strong> {pickupAddress}</p>
                        <p><strong>Estimated fee:</strong> {estimatedPrice:N0} VND</p>
                    </div>
                    
                    <div style='background-color: #FFF3E0; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <h3 style='color: #F57C00; margin-top: 0;'>🚌 Next steps:</h3>
                        <ol>
                            <li>Log in using your current account</li>
                            <li>Track bus schedules for your child</li>
                            <li>Contact us if you need any assistance</li>
                        </ol>
                    </div>
                    
                    <p>Thank you for choosing our service!</p>
                    
                    <p>Best regards,<br>
                    <strong>EduBus Team</strong></p>
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
            var subject = "❌ Pickup point request update";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #D32F2F;'>❌ Your pickup point request could not be approved</h2>
                    
                    <p>Hello <strong>{firstName} {lastName}</strong>,</p>
                    
                    <p>We regret to inform you that your request to use the student pickup service cannot be approved at this time.</p>
                    
                    <div style='background-color: #FFEBEE; padding: 15px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #D32F2F;'>
                        <h3 style='color: #D32F2F; margin-top: 0;'>📋 Request details:</h3>
                        <p><strong>Pickup address:</strong> {pickupAddress}</p>
                        <p><strong>Reason:</strong> {reason}</p>
                    </div>
                    
                    <div style='background-color: #E3F2FD; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <h3 style='color: #1976D2; margin-top: 0;'>💡 Suggestions:</h3>
                        <ul>
                            <li>Please review your request details</li>
                            <li>Contact us for further assistance</li>
                            <li>You can resubmit after addressing the noted issues</li>
                        </ul>
                    </div>
                    
                    <div style='background-color: #FFF3E0; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <h3 style='color: #F57C00; margin-top: 0;'>📞 Support:</h3>
                        <p>If you have any questions, please contact us:</p>
                        <ul>
                            <li>Email: support@edubus.com</li>
                            <li>Hotline: 1900-xxxx</li>
                            <li>Hours: 8:00 - 17:00 (Mon - Fri)</li>
                        </ul>
                    </div>
                    
                    <p>Thank you for your interest in our service!</p>
                    
                    <p>Best regards,<br>
                    <strong>EduBus Team</strong></p>
                </div>";

            await _email.SendEmailAsync(email, subject, body);
        }

        

        public async Task<List<PickupPointWithStudentStatusDto>> GetPickupPointsWithStudentStatusAsync()
        {
            var pickupPointsWithCount = await _pickupPointRepo.GetPickupPointsWithStudentCountAsync();
            
            var result = new List<PickupPointWithStudentStatusDto>();

            foreach (var (pickupPoint, assignedStudentCount) in pickupPointsWithCount)
            {
                // Get ALL assigned students for this pickup point (all statuses)
                var allAssignedStudents = await _studentRepo.GetQueryable()
                    .Where(s => !s.IsDeleted && s.CurrentPickupPointId == pickupPoint.Id)
                    .Select(s => new StudentInfo
                    {
                        Id = s.Id,
                        FirstName = s.FirstName,
                        LastName = s.LastName,
                        Status = s.Status,
                        PickupPointAssignedAt = s.PickupPointAssignedAt
                    })
                    .ToListAsync();

                // Calculate status breakdown
                var totalStudents = allAssignedStudents.Count;
                var activeStudents = allAssignedStudents.Count(s => s.Status == StudentStatus.Active);
                var pendingStudents = allAssignedStudents.Count(s => s.Status == StudentStatus.Pending);
                var inactiveStudents = allAssignedStudents.Count(s => s.Status == StudentStatus.Inactive);

                result.Add(new PickupPointWithStudentStatusDto
                {
                    Id = pickupPoint.Id,
                    Description = pickupPoint.Description,
                    Location = pickupPoint.Location,
                    Latitude = pickupPoint.Geog != null ? pickupPoint.Geog.Y : null,
                    Longitude = pickupPoint.Geog != null ? pickupPoint.Geog.X : null,
                    CreatedAt = pickupPoint.CreatedAt,
                    UpdatedAt = pickupPoint.UpdatedAt,
                    IsDeleted = pickupPoint.IsDeleted,
                    AssignedStudentCount = assignedStudentCount,
                    AssignedStudents = allAssignedStudents,
                    TotalStudents = totalStudents,
                    ActiveStudents = activeStudents,
                    PendingStudents = pendingStudents,
                    InactiveStudents = inactiveStudents
                });
            }

            return result;
        }

        public async Task AssignPickupPointAfterPaymentAsync(Guid transactionId)
        {
            // Get the transaction
            var transaction = await _transactionRepo.FindAsync(transactionId);
            if (transaction == null)
            {
                throw new KeyNotFoundException($"Transaction with ID {transactionId} not found.");
            }

            // Check if transaction has a pickup point request ID
            if (string.IsNullOrEmpty(transaction.PickupPointRequestId))
            {
                // No pickup point request associated with this transaction, skip assignment
                return;
            }

            // Get the pickup point request from MongoDB
            var pickupPointRequestId = Guid.Parse(transaction.PickupPointRequestId);
            var pickupPointRequest = await _requestRepo.FindAsync(pickupPointRequestId);
            if (pickupPointRequest == null)
            {
                throw new KeyNotFoundException($"Pickup point request with ID {pickupPointRequestId} not found.");
            }

            // Check if pickup point was created (should exist after approval)
            if (!pickupPointRequest.PickupPointId.HasValue)
            {
                throw new InvalidOperationException($"Pickup point has not been created for request {pickupPointRequestId}.");
            }

            var pickupPointId = pickupPointRequest.PickupPointId.Value;

            // Get students from transport fee items
            var transportFeeItems = await _transportFeeItemRepo.FindByConditionAsync(tfi => tfi.TransactionId == transactionId);
            var studentIds = transportFeeItems.Select(tfi => tfi.StudentId).Distinct().ToList();

            if (!studentIds.Any())
            {
                // No students associated with this transaction, skip assignment
                return;
            }

            // Assign pickup point to each student
            var now = DateTime.UtcNow;
            foreach (var studentId in studentIds)
            {
                var student = await _studentRepo.FindAsync(studentId);
                if (student == null || student.IsDeleted) continue;

                // Update student's current pickup point
                student.CurrentPickupPointId = pickupPointId;
                student.PickupPointAssignedAt = now;
                await _studentRepo.UpdateAsync(student);

                // Create StudentPickupPoint history record
                await _historyRepo.AddAsync(new StudentPickupPoint
                {
                    StudentId = studentId,
                    PickupPointId = pickupPointId,
                    AssignedAt = now,
                    ChangeReason = "Assigned after successful payment",
                    ChangedBy = $"Transaction:{transactionId}",
                    // Semester information from pickup point request
                    SemesterName = pickupPointRequest.SemesterName,
                    SemesterCode = pickupPointRequest.SemesterCode,
                    AcademicYear = pickupPointRequest.AcademicYear,
                    SemesterStartDate = pickupPointRequest.SemesterStartDate,
                    SemesterEndDate = pickupPointRequest.SemesterEndDate,
                    TotalSchoolDays = pickupPointRequest.TotalSchoolDays
                });
            }
        }
    }
}
