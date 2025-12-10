using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using Services.Contracts;
using Services.Models.Transaction;
using Microsoft.EntityFrameworkCore;

namespace Services.Implementations
{
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _transactionRepo;
        private readonly ITransportFeeItemService _transportFeeItemService;
        private readonly ITransportFeeItemRepository _transportFeeItemRepo;
        private readonly IStudentRepository _studentRepo;
        private readonly IParentRepository _parentRepo;
        private readonly IUnitPriceRepository _unitPriceRepo;
        private readonly IAcademicCalendarRepository _academicCalendarRepo;
        private readonly IScheduleRepository _scheduleRepo;
        private readonly IMultiStudentPolicyService _policyService;
        private readonly DbContext _dbContext;

        public TransactionService(
            ITransactionRepository transactionRepo,
            ITransportFeeItemService transportFeeItemService,
            ITransportFeeItemRepository transportFeeItemRepo,
            IStudentRepository studentRepo,
            IParentRepository parentRepo,
            IUnitPriceRepository unitPriceRepo,
            IAcademicCalendarRepository academicCalendarRepo,
            IScheduleRepository scheduleRepo,
            IMultiStudentPolicyService policyService,
            DbContext dbContext)
        {
            _transactionRepo = transactionRepo;
            _transportFeeItemService = transportFeeItemService;
            _transportFeeItemRepo = transportFeeItemRepo;
            _studentRepo = studentRepo;
            _parentRepo = parentRepo;
            _unitPriceRepo = unitPriceRepo;
            _academicCalendarRepo = academicCalendarRepo;
            _scheduleRepo = scheduleRepo;
            _policyService = policyService;
            _dbContext = dbContext;
        }

    public async Task<CreateTransactionFromPickupPointResponse> CreateTransactionFromPickupPointAsync(
        CreateTransactionFromPickupPointRequest request)
    {
        // Validate request
        if (request.StudentIds == null || !request.StudentIds.Any())
            throw new ArgumentException("Student IDs cannot be empty");

        if (request.DistanceKm <= 0)
            throw new ArgumentException("Distance must be greater than 0");

        if (request.UnitPricePerKm <= 0)
            throw new ArgumentException("Unit price must be greater than 0");

        // Get next semester info (for description and transport fee item details)
        var semesterInfo = await GetNextSemesterAsync();

        // Calculate fee per student (TotalFee)
        var feePerStudent = request.TotalFee;

        // Use execution strategy for retryable operations
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var transportFeeItems = new List<TransportFeeItemInfo>();
                var transactions = new List<TransactionInfo>();
                var students = await _studentRepo.GetQueryable()
                    .Where(s => request.StudentIds.Contains(s.Id))
                    .ToListAsync();

                // Create separate transaction for each student
                foreach (var student in students)
                {
                    // Create transaction for this student
                    var transactionEntity = new Transaction
                    {
                        Id = Guid.NewGuid(),
                        ParentId = request.ParentId,
                        TransactionCode = GenerateTransactionCode(),
                        Status = TransactionStatus.Notyet,
                        Amount = feePerStudent,
                        Currency = "VND",
                        Description = $"Transport fee for {student.FirstName} {student.LastName} - Semester {semesterInfo.Name} {semesterInfo.AcademicYear}",
                        Provider = PaymentProvider.PayOS,
                        PickupPointRequestId = request.PickupPointRequestId.ToString(),
                        CreatedAt = DateTime.UtcNow
                    };

                    await _transactionRepo.AddAsync(transactionEntity);

                    // Add transaction info
                    transactions.Add(new TransactionInfo
                    {
                        TransactionId = transactionEntity.Id,
                        TransactionCode = transactionEntity.TransactionCode,
                        StudentId = student.Id,
                        StudentName = $"{student.FirstName} {student.LastName}",
                        Amount = feePerStudent,
                        Description = transactionEntity.Description
                    });

                    // Create transport fee item for this student
                    var createItemRequest = new Services.Models.TransportFeeItem.CreateTransportFeeItemRequest
                    {
                        TransactionId = transactionEntity.Id,
                        StudentId = student.Id,
                        ParentEmail = request.ParentEmail,
                        Description = $"Transport fee for {student.FirstName} {student.LastName} - Semester {semesterInfo.Name}",
                        DistanceKm = request.DistanceKm,
                        UnitPricePerKm = request.UnitPricePerKm,
                        Subtotal = request.TotalFee, // TotalFee is already per student
                        UnitPriceId = request.UnitPriceId,
                        SemesterName = semesterInfo.Name,
                        AcademicYear = semesterInfo.AcademicYear,
                        SemesterStartDate = semesterInfo.StartDate,
                        SemesterEndDate = semesterInfo.EndDate,
                        TotalSchoolDays = 0, // Not needed for individual item calculation
                        Type = TransportFeeItemType.Register
                    };

                    var transportFeeItem = await _transportFeeItemService.CreateAsync(createItemRequest);

                    transportFeeItems.Add(new TransportFeeItemInfo
                    {
                        Id = transportFeeItem.Id,
                        StudentId = student.Id,
                        StudentName = $"{student.FirstName} {student.LastName}",
                        Amount = transportFeeItem.Subtotal,
                        Description = transportFeeItem.Description
                    });
                }

                await transaction.CommitAsync();

                return new CreateTransactionFromPickupPointResponse
                {
                    Transactions = transactions,
                    TotalAmount = request.TotalFee,
                    TransportFeeItems = transportFeeItems,
                    Message = $"Created {students.Count} separate transactions for {students.Count} student(s)"
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

        public async Task<TransactionDetailResponseDto> GetTransactionDetailAsync(Guid transactionId)
        {
            var transaction = await _transactionRepo.GetQueryable()
                .FirstOrDefaultAsync(t => t.Id == transactionId && !t.IsDeleted);
            if (transaction == null)
                throw new KeyNotFoundException("Transaction not found");

            // Get full TransportFeeItem details instead of summaries
            var transportFeeItems = await _transportFeeItemRepo.GetByTransactionIdAsync(transactionId);
            var studentIds = transportFeeItems.Select(i => i.StudentId).ToList();
            var students = await _studentRepo.GetQueryable()
                .Where(s => studentIds.Contains(s.Id))
                .ToListAsync();

            var transportFeeItemDetails = transportFeeItems.Select(item =>
            {
                var student = students.FirstOrDefault(s => s.Id == item.StudentId);
                return new TransportFeeItemDetail
                {
                    Id = item.Id,
                    StudentId = item.StudentId,
                    StudentName = student != null ? $"{student.FirstName} {student.LastName}" : "",
                    Description = item.Description,
                    DistanceKm = item.DistanceKm,
                    UnitPricePerKm = item.UnitPriceVndPerKm,
                    Amount = item.Subtotal,
                    SemesterName = item.SemesterName,
                    AcademicYear = item.AcademicYear,
                    Type = item.Type,
                    Status = item.Status
                };
            }).ToList();

            return new TransactionDetailResponseDto
            {
                Id = transaction.Id,
                ParentId = transaction.ParentId,
                ParentEmail = "", // Will be populated from parent service if needed
                TransactionCode = transaction.TransactionCode,
                Status = transaction.Status,
                Amount = transaction.Amount,
                Currency = transaction.Currency,
                Description = transaction.Description,
                Provider = transaction.Provider,
                CreatedAt = transaction.CreatedAt,
                PaidAt = transaction.PaidAtUtc,
                TransportFeeItems = transportFeeItemDetails
            };
        }

        public async Task<TransactionDetailResponseDto> GetTransactionByTransportFeeItemIdAsync(Guid transportFeeItemId)
        {
            var transaction = await _transactionRepo.GetTransactionByTransportFeeItemIdAsync(transportFeeItemId);
            if (transaction == null)
                throw new KeyNotFoundException("Transaction not found for the given transport fee item ID");

            // Get full TransportFeeItem details instead of summaries
            var transportFeeItems = await _transportFeeItemRepo.GetByTransactionIdAsync(transaction.Id);
            var studentIds = transportFeeItems.Select(i => i.StudentId).ToList();
            var students = await _studentRepo.GetQueryable()
                .Where(s => studentIds.Contains(s.Id))
                .ToListAsync();

            var transportFeeItemDetails = transportFeeItems.Select(item =>
            {
                var student = students.FirstOrDefault(s => s.Id == item.StudentId);
                return new TransportFeeItemDetail
                {
                    Id = item.Id,
                    StudentId = item.StudentId,
                    StudentName = student != null ? $"{student.FirstName} {student.LastName}" : "",
                    Description = item.Description,
                    DistanceKm = item.DistanceKm,
                    UnitPricePerKm = item.UnitPriceVndPerKm,
                    Amount = item.Subtotal,
                    SemesterName = item.SemesterName,
                    AcademicYear = item.AcademicYear,
                    Type = item.Type,
                    Status = item.Status
                };
            }).ToList();

            return new TransactionDetailResponseDto
            {
                Id = transaction.Id,
                ParentId = transaction.ParentId,
                ParentEmail = "", // Will be populated from parent service if needed
                TransactionCode = transaction.TransactionCode,
                Status = transaction.Status,
                Amount = transaction.Amount,
                Currency = transaction.Currency,
                Description = transaction.Description,
                Provider = transaction.Provider,
                CreatedAt = transaction.CreatedAt,
                PaidAt = transaction.PaidAtUtc,
                TransportFeeItems = transportFeeItemDetails
            };
        }

        public async Task<TransactionListResponseDto> GetTransactionListAsync(TransactionListRequest request)
        {
            // Business logic validation
            if (request.FromDate.HasValue && request.ToDate.HasValue && request.FromDate.Value > request.ToDate.Value)
                throw new ArgumentException("FromDate cannot be greater than ToDate");

            var query = _transactionRepo.GetQueryable().Where(t => !t.IsDeleted);

            // Apply filters
            if (request.ParentId.HasValue)
                query = query.Where(t => t.ParentId == request.ParentId.Value);

            if (request.Status.HasValue)
                query = query.Where(t => t.Status == request.Status.Value);

            if (!string.IsNullOrWhiteSpace(request.TransactionCode))
                query = query.Where(t => t.TransactionCode.Contains(request.TransactionCode));

            if (request.FromDate.HasValue)
                query = query.Where(t => t.CreatedAt >= request.FromDate.Value);

            if (request.ToDate.HasValue)
                query = query.Where(t => t.CreatedAt <= request.ToDate.Value);

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var transactions = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            // Map to DTOs
            var transactionSummaries = transactions.Select(t => new TransactionSummary
            {
                Id = t.Id,
                TransactionCode = t.TransactionCode,
                Status = t.Status,
                Amount = t.Amount,
                Currency = t.Currency,
                Description = t.Description,
                CreatedAt = t.CreatedAt,
                PaidAtUtc = t.PaidAtUtc,
                ParentId = t.ParentId,
                StudentCount = 0 // Will be calculated separately if needed
            }).ToList();

            return new TransactionListResponseDto
            {
                Transactions = transactionSummaries,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async Task<TransactionListResponseDto> GetTransactionsByStudentAsync(Guid studentId, int page, int pageSize)
        {
            // Business logic validation
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            // Get transactions by student from repository
            var (transactions, totalCount) = await _transactionRepo.GetTransactionsByStudentAsync(studentId, page, pageSize);

            // Map to DTOs
            var transactionSummaries = transactions.Select(t => new TransactionSummary
            {
                Id = t.Id,
                TransactionCode = t.TransactionCode,
                Status = t.Status,
                Amount = t.Amount,
                Currency = t.Currency,
                Description = t.Description,
                CreatedAt = t.CreatedAt,
                PaidAtUtc = t.PaidAtUtc,
                ParentId = t.ParentId
            }).ToList();

            return new TransactionListResponseDto
            {
                Transactions = transactionSummaries,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<bool> UpdateTransactionStatusAsync(Guid transactionId, TransactionStatus status)
        {
            var transaction = await _transactionRepo.FindAsync(transactionId);
            if (transaction == null)
                return false;

            transaction.Status = status;
            if (status == TransactionStatus.Paid)
            {
                transaction.PaidAtUtc = DateTime.UtcNow;
            }

            await _transactionRepo.UpdateAsync(transaction);
            return true;
        }

        public async Task<bool> DeleteTransactionAsync(Guid transactionId)
        {
            var transaction = await _transactionRepo.GetQueryable()
                .FirstOrDefaultAsync(t => t.Id == transactionId && !t.IsDeleted);
            if (transaction == null)
                throw new KeyNotFoundException("Transaction not found or has been deleted");

            transaction.IsDeleted = true;
            transaction.UpdatedAt = DateTime.UtcNow;

            await _transactionRepo.UpdateAsync(transaction);
            return true;
        }

        public async Task<bool> UpdateTransactionAsync(Guid transactionId, dynamic request)
        {
            var transaction = await _transactionRepo.GetQueryable()
                .FirstOrDefaultAsync(t => t.Id == transactionId && !t.IsDeleted);
            if (transaction == null)
                throw new KeyNotFoundException("Transaction not found or has been deleted");

            // Update only provided fields
            if (request.Description != null)
                transaction.Description = request.Description;

            if (request.Amount != null)
                transaction.Amount = request.Amount;

            if (request.Currency != null)
                transaction.Currency = request.Currency;

            transaction.UpdatedAt = DateTime.UtcNow;

            await _transactionRepo.UpdateAsync(transaction);
            return true;
        }

        public async Task<CalculateFeeResponse> CalculateTransportFeeAsync(CalculateFeeRequest request)
        {
            // 1. Get current active unit price
            var unitPrice = await GetCurrentActiveUnitPriceAsync(request.UnitPriceId);
            
            // 2. Get next semester info
            var semesterInfo = await GetNextSemesterAsync();
            
            // 3. Calculate school days (excluding weekends and holidays)
            var totalSchoolDays = CalculateSchoolDays(semesterInfo.StartDate, semesterInfo.EndDate, semesterInfo.Holidays, semesterInfo.AcademicYear);
            var totalTrips = totalSchoolDays * 2; // Round trip per day (for display only)
            
            // 4. Calculate total distance for the semester (unit price is per day, not per trip)
            var totalDistanceKm = request.DistanceKm * totalSchoolDays;
            
            // 5. Calculate ORIGINAL fee (before policy)
            var originalFee = unitPrice.PricePerKm * (decimal)totalDistanceKm;
            
            // 6. Calculate policy reduction
            var studentCount = request.StudentCount > 0 ? request.StudentCount : 1;
            var policyResult = await _policyService.CalculatePolicyAsync(studentCount, originalFee);
            
            // 7. Calculate final fee after policy
            var totalFee = originalFee - policyResult.ReductionAmount;
            
            // 8. Build calculation details
            var calculationDetails = $"Transport fee for {semesterInfo.Name} {semesterInfo.AcademicYear}:\n" +
                                   $"- Distance: {request.DistanceKm} km\n" +
                                   $"- Unit price: {unitPrice.PricePerKm:N0} VND/km\n" +
                                   $"- School days: {totalSchoolDays} days (excluding weekends and holidays)\n" +
                                   $"- Total trips: {totalTrips} trips (round trip per day)\n" +
                                   $"- Total distance: {totalDistanceKm:N1} km\n" +
                                   $"- Original fee: {originalFee:N0} VND\n" +
                                   $"- Student count: {studentCount}\n";
            
            if (policyResult.ReductionAmount > 0)
            {
                calculationDetails += $"- Policy reduction: {policyResult.ReductionPercentage}% ({policyResult.ReductionAmount:N0} VND)\n" +
                                     $"- Policy: {policyResult.Description}\n";
            }
            
            calculationDetails += $"- Final fee: {totalFee:N0} VND";
            
            return new CalculateFeeResponse
            {
                TotalFee = totalFee,
                OriginalFee = originalFee,
                PolicyReductionAmount = policyResult.ReductionAmount,
                PolicyReductionPercentage = policyResult.ReductionPercentage,
                PolicyDescription = policyResult.Description,
                UnitPricePerKm = unitPrice.PricePerKm,
                DistanceKm = request.DistanceKm,
                TotalSchoolDays = totalSchoolDays,
                TotalTrips = totalTrips,
                TotalDistanceKm = totalDistanceKm,
                StudentCount = studentCount,
                SemesterName = semesterInfo.Name,
                AcademicYear = semesterInfo.AcademicYear,
                SemesterStartDate = semesterInfo.StartDate,
                SemesterEndDate = semesterInfo.EndDate,
                Holidays = semesterInfo.Holidays,
                CalculationDetails = calculationDetails
            };
        }

        public async Task<AcademicSemesterInfo> GetNextSemesterAsync()
        {
            var academicCalendars = await _academicCalendarRepo.GetActiveAsync();
            var currentDate = DateTime.UtcNow;

            foreach (var calendar in academicCalendars)
            {
                // First, check if there's a semester currently running
                var currentSemester = calendar.Semesters
                    .Where(s => s.IsActive && s.StartDate <= currentDate && s.EndDate >= currentDate)
                    .FirstOrDefault();

                AcademicSemester? targetSemester = null;

                if (currentSemester != null)
                {
                    // If there's a current semester, get the next one
                    targetSemester = calendar.Semesters
                        .Where(s => s.IsActive && s.StartDate > currentSemester.EndDate)
                        .OrderBy(s => s.StartDate)
                        .FirstOrDefault();
                }
                else
                {
                    // If no current semester, get the next upcoming semester
                    targetSemester = calendar.Semesters
                        .Where(s => s.IsActive && s.StartDate > currentDate)
                        .OrderBy(s => s.StartDate)
                        .FirstOrDefault();
                }

                if (targetSemester != null)
                {
                    return new AcademicSemesterInfo
                    {
                        Name = targetSemester.Name,
                        Code = targetSemester.Code,
                        AcademicYear = calendar.AcademicYear,
                        StartDate = targetSemester.StartDate,
                        EndDate = targetSemester.EndDate,
                        Holidays = GetHolidayDates(calendar.Holidays, targetSemester.StartDate, targetSemester.EndDate),
                        TotalSchoolDays = CalculateSchoolDays(targetSemester.StartDate, targetSemester.EndDate, GetHolidayDates(calendar.Holidays, targetSemester.StartDate, targetSemester.EndDate), calendar.AcademicYear),
                        TotalTrips = CalculateSchoolDays(targetSemester.StartDate, targetSemester.EndDate, GetHolidayDates(calendar.Holidays, targetSemester.StartDate, targetSemester.EndDate), calendar.AcademicYear) * 2
                    };
                }
            }

            throw new InvalidOperationException("No upcoming semester found");
        }

        private List<DateTime> GetHolidayDates(List<SchoolHoliday> holidays, DateTime semesterStart, DateTime semesterEnd)
        {
            var holidayDates = new List<DateTime>();
            
            foreach (var holiday in holidays)
            {
                var currentDate = holiday.StartDate.Date; // Use Date to normalize time
                var endDate = holiday.EndDate.Date; // Use Date to normalize time
                
                while (currentDate <= endDate)
                {
                    // Only include holidays that fall within the semester period
                    if (currentDate >= semesterStart.Date && currentDate <= semesterEnd.Date)
                    {
                        holidayDates.Add(currentDate);
                    }
                    currentDate = currentDate.AddDays(1);
                }
            }
            
            return holidayDates;
        }

        private int CalculateSchoolDays(DateTime startDate, DateTime endDate, List<DateTime> holidays, string academicYear)
        {
            var schoolDays = 0;
            var currentDate = startDate;

            // Get schedule for the academic year to determine which days of week are school days
            var schedule = GetScheduleForAcademicYear(academicYear);
            var schoolDaysOfWeek = ParseRRuleToDaysOfWeek(schedule?.RRule);

            while (currentDate <= endDate)
            {
                // Check if current day is a school day based on RRule
                if (schoolDaysOfWeek.Contains(currentDate.DayOfWeek))
                {
                    // Skip holidays - check if current date falls within any holiday period
                    var isHoliday = holidays.Any(h => h.Date == currentDate.Date);
                    if (!isHoliday)
                    {
                        schoolDays++;
                    }
                }
                currentDate = currentDate.AddDays(1);
            }

            return schoolDays;
        }

        private Schedule? GetScheduleForAcademicYear(string academicYear)
        {
            var schedules = _scheduleRepo.GetActiveSchedulesAsync().Result;
            
            // Find schedule that matches academic year AND is effective during the semester period
            return schedules.FirstOrDefault(s => 
                s.AcademicYear == academicYear && 
                s.IsActive &&
                s.EffectiveFrom <= DateTime.UtcNow && // Schedule is already effective
                (s.EffectiveTo == null || s.EffectiveTo >= DateTime.UtcNow) // Schedule hasn't expired
            );
        }

        private List<DayOfWeek> ParseRRuleToDaysOfWeek(string? rrule)
        {
            if (string.IsNullOrEmpty(rrule))
            {
                // Default to Monday-Friday if no RRule specified
                return new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
            }

            var daysOfWeek = new List<DayOfWeek>();
            
            // Parse RRule to extract days of week
            // Example RRule: "FREQ=WEEKLY;BYDAY=MO,TU,WE,TH,FR"
            if (rrule.Contains("BYDAY"))
            {
                var byDayPart = rrule.Split(';').FirstOrDefault(p => p.StartsWith("BYDAY="));
                if (byDayPart != null)
                {
                    var days = byDayPart.Split('=')[1].Split(',');
                    foreach (var day in days)
                    {
                        switch (day.Trim())
                        {
                            case "MO": daysOfWeek.Add(DayOfWeek.Monday); break;
                            case "TU": daysOfWeek.Add(DayOfWeek.Tuesday); break;
                            case "WE": daysOfWeek.Add(DayOfWeek.Wednesday); break;
                            case "TH": daysOfWeek.Add(DayOfWeek.Thursday); break;
                            case "FR": daysOfWeek.Add(DayOfWeek.Friday); break;
                            case "SA": daysOfWeek.Add(DayOfWeek.Saturday); break;
                            case "SU": daysOfWeek.Add(DayOfWeek.Sunday); break;
                        }
                    }
                }
            }

            // If no days found, default to Monday-Friday
            if (!daysOfWeek.Any())
            {
                daysOfWeek = new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
            }

            return daysOfWeek;
        }

        private string GenerateTransactionCode()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(1000, 9999);
            return $"TXN{timestamp}{random}";
        }

        public async Task<UnitPrice> GetCurrentActiveUnitPriceAsync(Guid? unitPriceId = null)
        {
            if (unitPriceId.HasValue)
            {
                var specificUnitPrice = await _unitPriceRepo.FindAsync(unitPriceId.Value);
                if (specificUnitPrice != null && specificUnitPrice.IsActive && 
                    specificUnitPrice.EffectiveFrom <= DateTime.UtcNow && 
                    specificUnitPrice.EffectiveTo >= DateTime.UtcNow)
                {
                    return specificUnitPrice;
                }
            }

            // Get current active unit price
            var activeUnitPrices = await _unitPriceRepo.GetQueryable()
                .Where(up => up.IsActive && 
                           up.EffectiveFrom <= DateTime.UtcNow && 
                           up.EffectiveTo >= DateTime.UtcNow)
                .OrderByDescending(up => up.EffectiveFrom)
                .ToListAsync();

            if (!activeUnitPrices.Any())
            {
                throw new InvalidOperationException("No active unit price found. Please contact administrator to set up unit price.");
            }

            return activeUnitPrices.First();
        }

        /// <summary>
        /// Admin creates a transaction for parent to pay semester fee
        /// For new parent registration workflow
        /// </summary>
        public async Task<AdminCreateTransactionResponse> AdminCreateTransactionAsync(
            AdminCreateTransactionRequest request, Guid adminId)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Validate parent exists
            var parent = await _parentRepo.FindAsync(request.ParentId);
            if (parent == null)
                throw new KeyNotFoundException($"Parent with ID {request.ParentId} not found");

            // Validate all students exist and belong to the parent
            var students = new List<Student>();
            foreach (var studentId in request.StudentIds)
            {
                var student = await _studentRepo.FindAsync(studentId);
                if (student == null)
                    throw new KeyNotFoundException($"Student with ID {studentId} not found");

                if (student.ParentId != request.ParentId)
                    throw new InvalidOperationException(
                        $"Student {student.FirstName} {student.LastName} does not belong to the specified parent");

                students.Add(student);
            }

            // Get semester info for transport fee items
            var semesterInfo = await GetNextSemesterAsync();

            // Use execution strategy for transaction
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _dbContext.Database.BeginTransactionAsync();
                try
                {
                    // Create ONE transaction for all students
                    var transactionEntity = new Transaction
                    {
                        Id = Guid.NewGuid(),
                        ParentId = request.ParentId,
                        TransactionCode = GenerateTransactionCode(),
                        Status = TransactionStatus.Notyet, // Pending payment (Notyet = Pending in this system)
                        Amount = request.Amount,
                        Currency = "VND",
                        Description = request.Description,
                        Provider = PaymentProvider.PayOS,
                        CreatedAt = DateTime.UtcNow
                        // Note: DueDate field doesn't exist in Transaction model
                        // Consider adding it in future database migration
                    };

                    await _transactionRepo.AddAsync(transactionEntity);

                    // Create transport fee items for each student
                    var transportFeeItems = new List<TransportFeeItemInfo>();
                    var feePerStudent = request.Amount / request.StudentIds.Count; // Divide total amount by number of students

                    foreach (var student in students)
                    {
                        var createItemRequest = new Services.Models.TransportFeeItem.CreateTransportFeeItemRequest
                        {
                            TransactionId = transactionEntity.Id,
                            StudentId = student.Id,
                            ParentEmail = parent.Email,
                            Description = $"Transport fee for {student.FirstName} {student.LastName} - {semesterInfo.Name} {semesterInfo.AcademicYear}",
                            DistanceKm = request.DistanceKm,
                            UnitPricePerKm = request.UnitPricePerKm,
                            Subtotal = feePerStudent,
                            UnitPriceId = Guid.Empty, // Will be set by TransportFeeItemService if needed
                            SemesterName = semesterInfo.Name,
                            AcademicYear = semesterInfo.AcademicYear,
                            SemesterStartDate = semesterInfo.StartDate,
                            SemesterEndDate = semesterInfo.EndDate,
                            TotalSchoolDays = request.Metadata?.TotalSchoolDays ?? semesterInfo.TotalSchoolDays,
                            Type = TransportFeeItemType.Register
                        };

                        var transportFeeItem = await _transportFeeItemService.CreateAsync(createItemRequest);

                        transportFeeItems.Add(new TransportFeeItemInfo
                        {
                            Id = transportFeeItem.Id,
                            StudentId = student.Id,
                            StudentName = $"{student.FirstName} {student.LastName}",
                            Amount = transportFeeItem.Subtotal,
                            Description = transportFeeItem.Description
                        });
                    }

                    await transaction.CommitAsync();

                    return new AdminCreateTransactionResponse
                    {
                        Id = transactionEntity.Id,
                        ParentId = request.ParentId,
                        StudentIds = request.StudentIds,
                        TransactionCode = transactionEntity.TransactionCode,
                        Amount = request.Amount,
                        Status = transactionEntity.Status,
                        Description = request.Description,
                        DueDate = request.DueDate,
                        CreatedAt = transactionEntity.CreatedAt,
                        CreatedByAdminId = adminId,
                        TransportFeeItems = transportFeeItems,
                        Message = $"Transaction created successfully for {students.Count} student(s). Status: Pending payment."
                    };
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
    }
}
