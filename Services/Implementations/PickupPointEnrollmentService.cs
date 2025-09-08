using Data.Models;
using Data.Repos.Interfaces;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Services.Contracts;
using Services.Models.PickupPoint;

namespace Services.Implementations
{
    /// <summary>
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
        private readonly IEmailService _email;
        private readonly DbContext _sqlDb;
        private readonly IOtpStore _otpStore;

        private const string Purpose = "PickupPointRequest";
        private const decimal MaxEstimatedPrice = 50_000_000m; // sanity cap

        public PickupPointEnrollmentService(
            IStudentRepository studentRepo,
            IPickupPointRepository pickupPointRepo,
            IStudentPickupPointHistoryRepository historyRepo,
            IPickupPointRequestRepository requestRepo,
            IEmailService email,
            DbContext sqlDb,
            IOtpStore otpStore)
        {
            _studentRepo = studentRepo;
            _pickupPointRepo = pickupPointRepo;
            _historyRepo = historyRepo;
            _requestRepo = requestRepo;
            _email = email;
            _sqlDb = sqlDb;
            _otpStore = otpStore;
        }

        /// <summary>
        /// Check if there exists at least one student bound to the given parent email.
        /// </summary>
        public async Task<bool> CheckParentEmailExistsAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            return await _studentRepo.GetQueryable()
                .AnyAsync(s => s.ParentEmail == email && !s.IsDeleted);
        }

        /// <summary>
        /// Generate and email an OTP to the parent email.
        /// Throws InvalidOperationException if email not found or OTP still valid.
        /// </summary>
        public async Task SendOtpAsync(string email)
        {
            if (!await CheckParentEmailExistsAsync(email))
                throw new InvalidOperationException("The email does not exist or is not linked to any student.");

            var otp = GenerateOtp(6);
            var hash = BCrypt.Net.BCrypt.HashPassword(otp);

            // Save OTP to Redis with TTL and single-use constraint
            var saved = await _otpStore.SaveAsync(Purpose, email, hash, TimeSpan.FromMinutes(5), overwrite: false);
            if (!saved)
                throw new InvalidOperationException("An OTP is still valid. Please check your email or try again later.");

            // --- Email content (Vietnamese) ---
            var subject = "[EduBus] Mã xác thực OTP";
            var body = $@"
<p>Xin chào,</p>
<p>Mã OTP của bạn là <b>{otp}</b>, có hiệu lực trong <b>5 phút</b>.</p>
<p>Nếu bạn không yêu cầu thao tác này, vui lòng bỏ qua email.</p>
<p>Trân trọng,<br/>EduBus Team</p>";

            await _email.SendEmailAsync(email, subject, body);
        }

        /// <summary>
        /// Verify OTP against Redis value. Automatically deletes OTP on success or after max attempts.
        /// Return true on success; otherwise false.
        /// </summary>
        public async Task<bool> VerifyOtpAsync(string email, string otp)
        {
            var (hash, attempts, max) = await _otpStore.GetAsync(Purpose, email);
            if (hash is null) return false;

            if (attempts >= max)
            {
                await _otpStore.DeleteAsync(Purpose, email);
                return false;
            }

            var ok = BCrypt.Net.BCrypt.Verify(otp, hash);
            if (!ok)
            {
                var (allowed, _, _) = await _otpStore.IncrementAttemptsAsync(Purpose, email);
                if (!allowed) await _otpStore.DeleteAsync(Purpose, email);
                return false;
            }

            await _otpStore.DeleteAsync(Purpose, email);
            return true;
        }

        /// <summary>
        /// Return a minimal list of students bound to a validated parent email.
        /// </summary>
        public async Task<List<StudentBriefDto>> GetStudentsByEmailAsync(string email)
        {
            var students = await _studentRepo.GetStudentsByParentEmailAsync(email);
            return students
                .Select(s => new StudentBriefDto { Id = s.Id, FirstName = s.FirstName, LastName = s.LastName })
                .ToList();
        }

        /// <summary>
        /// Create a new pickup point request in Mongo after strict validations.
        /// Throws on invalid email, students mismatch or unreasonable price.
        /// </summary>
        public async Task<PickupPointRequestDocument> CreateRequestAsync(CreatePickupPointRequestDto dto)
        {
            if (!await CheckParentEmailExistsAsync(dto.ParentEmail))
                throw new InvalidOperationException("The provided email does not match any record in the system.");

            if (dto.StudentIds is null || dto.StudentIds.Count == 0)
                throw new ArgumentException("At least one student must be selected.", nameof(dto.StudentIds));

            var validStudents = await _studentRepo.GetQueryable()
                .Where(s => s.ParentEmail == dto.ParentEmail && dto.StudentIds.Contains(s.Id) && !s.IsDeleted)
                .Select(s => s.Id)
                .ToListAsync();

            if (validStudents.Count != dto.StudentIds.Count)
                throw new InvalidOperationException("The student list contains one or more invalid items.");

            if (dto.EstimatedPriceVnd <= 0 || dto.EstimatedPriceVnd > MaxEstimatedPrice)
                throw new ArgumentOutOfRangeException(nameof(dto.EstimatedPriceVnd), "Estimated price is out of allowed range.");

            var doc = new PickupPointRequestDocument
            {
                ParentEmail = dto.ParentEmail,
                StudentIds = validStudents,
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

            return await _requestRepo.AddAsync(doc);
        }

        /// <summary>
        /// List requests from Mongo with optional filters.
        /// </summary>
        public Task<List<PickupPointRequestDocument>> ListRequestsAsync(PickupPointRequestListQuery query)
            => _requestRepo.QueryAsync(query.Status, query.ParentEmail, query.Skip, query.Take);

        /// <summary>
        /// Approve a parent request:
        /// - Create PickupPoint in SQL
        /// - Close previous assignment (history) if any
        /// - Assign new PickupPoint and write history
        /// - Update Mongo request status
        /// All SQL changes happen in a transaction.
        /// </summary>
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

            // Update Mongo request status (outside SQL retry scope)
            req.Status = "Approved";
            req.ReviewedAt = DateTime.UtcNow;
            req.ReviewedByAdminId = adminId;
            req.AdminNotes = notes ?? "";
            await _requestRepo.UpdateAsync(req);
        }

        /// <summary>
        /// Reject a parent request and record reason in Mongo.
        /// </summary>
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
        }

        private static string GenerateOtp(int len)
        {
            var rnd = Random.Shared;
            return string.Concat(Enumerable.Range(0, len).Select(_ => rnd.Next(0, 10)));
        }
    }
}
