using AutoMapper;
using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Services.Contracts;
using Services.Models.RelocationRequest;
using static Data.Models.Transaction;

namespace Services.Implementations
{
	public class RelocationRequestService : IRelocationRequestService
	{
		private readonly IRelocationRequestRepository _relocationRepo;
		private readonly IStudentRepository _studentRepo;
		private readonly IStudentPickupPointRepository _historyRepo;
		private readonly IPickupPointRepository _pickupPointRepo;
		private readonly ITransactionRepository _transactionRepo;
		private readonly ITransportFeeItemRepository _feeItemRepo;
		private readonly ITransactionService _transactionService;
		private readonly IAcademicCalendarService _academicCalendarService;
		private readonly IVietMapService _vietMapService;
		private readonly ISchoolService _schoolService;
		private readonly IMapper _mapper;

		public RelocationRequestService(
			IRelocationRequestRepository relocationRepo,
			IStudentRepository studentRepo,
			IStudentPickupPointRepository historyRepo,
			IPickupPointRepository pickupPointRepo,
			ITransactionRepository transactionRepo,
			ITransportFeeItemRepository feeItemRepo,
			ITransactionService transactionService,
			IAcademicCalendarService academicCalendarService,
			IVietMapService vietMapService,
			ISchoolService schoolService,
			IMapper mapper)
		{
			_relocationRepo = relocationRepo;
			_studentRepo = studentRepo;
			_historyRepo = historyRepo;
			_pickupPointRepo = pickupPointRepo;
			_transactionRepo = transactionRepo;
			_feeItemRepo = feeItemRepo;
			_transactionService = transactionService;
			_academicCalendarService = academicCalendarService;
			_vietMapService = vietMapService;
			_schoolService = schoolService;
			_mapper = mapper;
		}

		public async Task<RelocationRequestResponseDto> CreateRequestAsync(
			CreateRelocationRequestDto dto,
			Guid parentId)
		{
			// 1. Validate student exists and belongs to parent
			var student = await _studentRepo.FindAsync(dto.StudentId);
			if (student == null || student.IsDeleted)
				throw new KeyNotFoundException("Student not found.");

			if (student.ParentId != parentId)
				throw new UnauthorizedAccessException("You are not authorized to request relocation for this student.");

			// 2. Get current semester info
			var semesterInfo = await _transactionService.GetNextSemesterAsync();

			// 3. Get student's current pickup point assignment for this semester
			var currentAssignment = await _historyRepo.GetQueryable()
				.Where(h => h.StudentId == dto.StudentId
					&& h.SemesterCode == semesterInfo.Code
					&& h.RemovedAt == null)
				.FirstOrDefaultAsync();

			if (currentAssignment == null)
				throw new InvalidOperationException("Student does not have a pickup point assignment for this semester.");

			// 4. Check for existing pending request
			var existingRequest = await _relocationRepo.GetPendingRequestForStudentAsync(
				dto.StudentId,
				semesterInfo.Code);

			if (existingRequest != null)
				throw new InvalidOperationException("Student already has a pending relocation request for this semester.");

			// 5. Get old pickup point details
			var oldPickupPoint = await _pickupPointRepo.FindAsync(currentAssignment.PickupPointId);
			if (oldPickupPoint == null)
				throw new InvalidOperationException("Current pickup point not found.");

			double oldDistanceKm = await CalculateDistanceFromSchoolAsync(oldPickupPoint.Geog);

			// 6. Calculate refund and financial impact
			var refundCalculation = await CalculateRefundAsync(
				dto.StudentId,
				currentAssignment.PickupPointId,
				DateTime.UtcNow,
				semesterInfo.Code,
				dto.Reason,
				oldDistanceKm,
				dto.NewDistanceKm);

			// 7. Generate AI recommendation
			var aiRecommendation = GenerateAIRecommendation(
				refundCalculation,
				dto.Reason,
				currentAssignment);

			// 8. Create relocation request document
			var document = new RelocationRequestDocument
			{
				RequestType = dto.UrgentRequest ? RelocationRequestType.Emergency : RelocationRequestType.Standard,
				RequestStatus = RelocationRequestStatus.Pending,
				Priority = dto.UrgentRequest ? "Urgent" : "Normal",

				ParentId = parentId,
				ParentEmail = student.ParentEmail ?? string.Empty,
				StudentId = dto.StudentId,
				StudentName = $"{student.FirstName} {student.LastName}",

				SemesterCode = semesterInfo.Code,
				SemesterName = semesterInfo.Name,
				AcademicYear = semesterInfo.AcademicYear,
				TotalSchoolDays = currentAssignment.TotalSchoolDays,
				DaysServiced = refundCalculation.DaysServiced,
				DaysRemaining = refundCalculation.DaysRemaining,

				OldPickupPointId = currentAssignment.PickupPointId,
				OldPickupPointAddress = oldPickupPoint.Location,
				OldDistanceKm = oldDistanceKm,

				NewPickupPointAddress = dto.NewPickupPointAddress,
				NewLatitude = dto.NewLatitude,
				NewLongitude = dto.NewLongitude,
				NewDistanceKm = dto.NewDistanceKm,

				OriginalPaymentAmount = refundCalculation.OriginalPayment,
				ValueServiced = refundCalculation.ValueServiced,
				ValueRemaining = refundCalculation.ValueRemaining,
				NewLocationCost = refundCalculation.NewLocationCost,
				RefundAmount = refundCalculation.NetRefund,
				AdditionalPaymentRequired = refundCalculation.AdditionalPaymentRequired,
				ProcessingFee = refundCalculation.ProcessingFee,
				UnitPricePerKm = await GetCurrentUnitPriceAsync(),

				Reason = dto.Reason,
				Description = dto.Description,
				EvidenceUrls = dto.EvidenceUrls,
				UrgentRequest = dto.UrgentRequest,
				RequestedEffectiveDate = dto.RequestedEffectiveDate,

				AIRecommendation = aiRecommendation
			};

			var created = await _relocationRepo.AddAsync(document);
			return MapToResponseDto(created);
		}

		public async Task<RefundCalculationResult> CalculateRefundPreviewAsync(
			Guid studentId,
			double newDistanceKm)
		{
			var semesterInfo = await _transactionService.GetNextSemesterAsync();

			var currentAssignment = await _historyRepo.GetQueryable()
				.Where(h => h.StudentId == studentId
					&& h.SemesterCode == semesterInfo.Code
					&& h.RemovedAt == null)
				.FirstOrDefaultAsync();

			if (currentAssignment == null)
				throw new InvalidOperationException("Student does not have a pickup point assignment for this semester.");

			var oldPickupPoint = await _pickupPointRepo.FindAsync(currentAssignment.PickupPointId);
			if (oldPickupPoint == null)
				throw new InvalidOperationException("Current pickup point not found.");

			double oldDistanceKm = await CalculateDistanceFromSchoolAsync(oldPickupPoint.Geog);

			return await CalculateRefundAsync(
				studentId,
				currentAssignment.PickupPointId,
				DateTime.UtcNow,
				semesterInfo.Code,
				RefundReason.FamilyRelocation,
				oldDistanceKm,
				newDistanceKm);
		}

		private async Task<RefundCalculationResult> CalculateRefundAsync(
			Guid studentId,
			Guid oldPickupPointId,
			DateTime requestDate,
			string semesterCode,
			string reason,
			double oldDistanceKm,
			double newDistanceKm)
		{
			// 1. Get student pickup point history
			var history = await _historyRepo.GetQueryable()
				.Where(h => h.StudentId == studentId
					&& h.SemesterCode == semesterCode
					&& h.RemovedAt == null)
				.FirstOrDefaultAsync();

			if (history == null)
				throw new InvalidOperationException("Student pickup point history not found.");

			// 2. Get original transaction
			var transaction = await _transactionRepo.GetQueryable()
				.Include(t => t.TransportFeeItems)
				.Where(t => t.ParentId == (Guid)_studentRepo.FindAsync(studentId).Result!.ParentId!
					&& t.Status == TransactionStatus.Paid)
				.OrderByDescending(t => t.CreatedAt)
				.FirstOrDefaultAsync();

			if (transaction == null)
				throw new InvalidOperationException("No paid transaction found for this student.");

			var feeItem = transaction.TransportFeeItems.FirstOrDefault(x => x.StudentId == studentId);
			if (feeItem == null)
				throw new InvalidOperationException("Fee item not found for student.");

			// 3. Calculate days serviced
			var daysServiced = await CalculateSchoolDaysAsync(
				history.SemesterStartDate,
				requestDate,
				history.AcademicYear);

			var daysRemaining = history.TotalSchoolDays - daysServiced;

			// 4. Calculate value breakdown
			var dailyRate = feeItem.Subtotal / history.TotalSchoolDays;
			var valueServiced = dailyRate * daysServiced;
			var valueRemaining = dailyRate * daysRemaining;

			// 5. Determine refund percentage based on policy
			var refundPercentage = GetRefundPercentage(
				daysServiced,
				history.TotalSchoolDays,
				requestDate,
				history.SemesterStartDate,
				reason);

			// 6. Calculate new location cost
			var unitPrice = await GetCurrentUnitPriceAsync();
			var newLocationCost = (decimal)newDistanceKm * unitPrice * daysRemaining;

			// 7. Calculate gross refund
			var grossRefund = valueRemaining * refundPercentage;

			// 8. Apply processing fee (if applicable)
			var processingFee = CalculateProcessingFee(grossRefund, reason);

			// 9. Net refund
			var netRefund = grossRefund - processingFee;

			// 10. Calculate additional payment required
			var additionalPaymentRequired = Math.Max(0, newLocationCost - valueRemaining);

			return new RefundCalculationResult
			{
				OriginalPayment = feeItem.Subtotal,
				TotalSchoolDays = history.TotalSchoolDays,
				DaysServiced = daysServiced,
				DaysRemaining = daysRemaining,
				ValueServiced = valueServiced,
				ValueRemaining = valueRemaining,
				RefundPercentage = refundPercentage,
				GrossRefund = grossRefund,
				ProcessingFee = processingFee,
				NetRefund = netRefund,
				Reason = reason,
				NewLocationCost = newLocationCost,
				AdditionalPaymentRequired = additionalPaymentRequired
			};
		}

		private async Task<int> CalculateSchoolDaysAsync(
			DateTime startDate,
			DateTime endDate,
			string academicYear)
		{
			int schoolDays = 0;
			var current = startDate.Date;

			// Get holidays from database
			var holidays = await GetHolidaysAsync(academicYear);

			while (current <= endDate.Date)
			{
				// Skip weekends
				if (current.DayOfWeek != DayOfWeek.Saturday &&
					current.DayOfWeek != DayOfWeek.Sunday)
				{
					// Skip holidays
					if (!holidays.Any(h => h.Date == current))
					{
						schoolDays++;
					}
				}
				current = current.AddDays(1);
			}

			return schoolDays;
		}

		private async Task<List<DateTime>> GetHolidaysAsync(string academicYear)
		{
			try
			{
				// Get academic calendar for the year
				var calendar = await _academicCalendarService.GetAcademicCalendarByYearAsync(academicYear);

				if (calendar == null || calendar.Holidays == null)
					return new List<DateTime>();

				// Extract all holiday dates
				var holidays = new List<DateTime>();
				foreach (var holiday in calendar.Holidays)
				{
					// Add all dates in the holiday range
					var current = holiday.StartDate.Date;
					while (current <= holiday.EndDate.Date)
					{
						holidays.Add(current);
						current = current.AddDays(1);
					}
				}

				return holidays.Distinct().ToList();
			}
			catch
			{
				// If calendar service fails, return empty list (graceful degradation)
				return new List<DateTime>();
			}
		}

		private decimal GetRefundPercentage(
			int daysServiced,
			int totalDays,
			DateTime requestDate,
			DateTime semesterStart,
			string reason)
		{
			var percentComplete = (decimal)daysServiced / totalDays * 100;

			// Special cases: Emergency/Medical/Safety - higher refund
			if (reason == RefundReason.Medical ||
				reason == RefundReason.Safety ||
				reason == RefundReason.FamilyEmergency)
			{
				return percentComplete switch
				{
					< 10 => 0.95m,  // 95% refund
					< 25 => 0.85m,  // 85% refund
					< 50 => 0.65m,  // 65% refund
					< 75 => 0.40m,  // 40% refund
					_ => 0.20m      // 20% refund (goodwill)
				};
			}

			// Normal cases: Relocation/Convenience
			return percentComplete switch
			{
				< 10 => 0.90m,  // 90% refund
				< 25 => 0.75m,  // 75% refund
				< 50 => 0.50m,  // 50% refund
				< 75 => 0.25m,  // 25% refund
				_ => 0.10m      // 10% refund (goodwill)
			};
		}

		private decimal CalculateProcessingFee(decimal grossRefund, string reason)
		{
			// Waive processing fee for emergency cases
			if (reason == RefundReason.Medical ||
				reason == RefundReason.Safety)
			{
				return 0;
			}

			// 5% processing fee, capped at 200,000 VND
			var fee = grossRefund * 0.05m;
			return Math.Min(fee, 200_000);
		}

		private AIRecommendation GenerateAIRecommendation(
			RefundCalculationResult refundCalc,
			string reason,
			StudentPickupPoint currentAssignment)
		{
			int score = 0;
			var reasons = new List<string>();
			var suggestedActions = new List<string>();
			var riskFactors = new List<string>();

			// Factor 1: Reason importance
			if (reason == RefundReason.FamilyRelocation)
			{
				score += 40;
				reasons.Add("✅ Valid reason: family relocation");
			}
			else if (reason == RefundReason.Medical || reason == RefundReason.Safety)
			{
				score += 50;
				reasons.Add("✅ Priority reason: health/safety concern");
			}
			else
			{
				score += 10;
				reasons.Add("⚠️ Convenience-based request");
			}

			// Factor 2: Timing
			var percentComplete = (decimal)refundCalc.DaysServiced / refundCalc.TotalSchoolDays * 100;
			if (percentComplete < 25)
			{
				score += 20;
				reasons.Add("✅ Early in semester");
			}
			else if (percentComplete < 50)
			{
				score += 10;
				reasons.Add("⚠️ Mid-semester");
			}
			else
			{
				score -= 10;
				reasons.Add("❌ Late in semester");
				riskFactors.Add("Request submitted late in semester");
			}

			// Factor 3: Financial impact
			if (refundCalc.AdditionalPaymentRequired > 0)
			{
				score -= 5;
				reasons.Add("⚠️ Requires additional payment from parent");
				suggestedActions.Add($"Send invoice for additional {refundCalc.AdditionalPaymentRequired:N0} VND");
			}
			else if (refundCalc.NetRefund > 0)
			{
				score += 10;
				reasons.Add("✅ New location is cheaper");
				suggestedActions.Add($"Process refund of {refundCalc.NetRefund:N0} VND");
			}
			else
			{
				score += 5;
				reasons.Add("➖ Price neutral");
			}

			// Generate recommendation
			string recommendation;
			string confidence;
			string summary;

			if (score >= 60)
			{
				recommendation = "APPROVE";
				confidence = "High";
				summary = "Strong case for approval";
				suggestedActions.Add("Update route assignments");
				suggestedActions.Add("Notify parent of approval");
			}
			else if (score >= 30)
			{
				recommendation = "REVIEW";
				confidence = "Medium";
				summary = "Requires careful consideration";
				suggestedActions.Add("Contact parent for more details");
				suggestedActions.Add("Assess route impact with operations team");
			}
			else
			{
				recommendation = "REJECT";
				confidence = "Low";
				summary = "Not recommended for approval";
				suggestedActions.Add("Explain rejection reasons to parent");
				suggestedActions.Add("Suggest deferring to next semester");
				riskFactors.Add("Low approval score based on multiple factors");
			}

			return new AIRecommendation
			{
				Recommendation = recommendation,
				Confidence = confidence,
				Score = score,
				Summary = summary,
				Reasons = reasons,
				SuggestedActions = suggestedActions,
				RiskFactors = riskFactors,
				CalculatedAt = DateTime.UtcNow
			};
		}

		public async Task<RelocationRequestResponseDto> ApproveRequestAsync(
			Guid requestId,
			ApproveRelocationRequestDto dto,
			Guid adminId)
		{
			var request = await _relocationRepo.FindAsync(requestId);
			if (request == null)
				throw new KeyNotFoundException("Relocation request not found.");

			if (request.RequestStatus != RelocationRequestStatus.Pending)
				throw new InvalidOperationException($"Cannot approve request with status: {request.RequestStatus}");

			// 1. Create new pickup point
			var newPickupPoint = new PickupPoint
			{
				Description = $"Relocation - {request.NewPickupPointAddress}",
				Location = request.NewPickupPointAddress,
				Geog = new Point(request.NewLongitude, request.NewLatitude) { SRID = 4326 }
			};
			await _pickupPointRepo.AddAsync(newPickupPoint);

			// 2. Update old StudentPickupPoint record (set RemovedAt)
			var oldAssignment = await _historyRepo.GetQueryable()
				.Where(h => h.StudentId == request.StudentId
					&& h.PickupPointId == request.OldPickupPointId
					&& h.SemesterCode == request.SemesterCode
					&& h.RemovedAt == null)
				.FirstOrDefaultAsync();

			if (oldAssignment != null)
			{
				oldAssignment.RemovedAt = DateTime.UtcNow;
				oldAssignment.ChangeReason = $"Relocated via request {requestId}";
				await _historyRepo.UpdateAsync(oldAssignment);
			}

			// 3. Create new StudentPickupPoint record
			var effectiveDate = dto.EffectiveDate ?? DateTime.UtcNow.AddDays(7);
			var newAssignment = new StudentPickupPoint
			{
				StudentId = request.StudentId,
				PickupPointId = newPickupPoint.Id,
				AssignedAt = DateTime.UtcNow,
				EffectiveDate = effectiveDate,
				ChangeReason = $"Approved relocation request {requestId}",
				ChangedBy = $"Admin:{adminId}",
				SemesterName = request.SemesterName,
				SemesterCode = request.SemesterCode,
				AcademicYear = request.AcademicYear,
				SemesterStartDate = oldAssignment?.SemesterStartDate ?? DateTime.UtcNow,
				SemesterEndDate = oldAssignment?.SemesterEndDate ?? DateTime.UtcNow.AddMonths(6),
				TotalSchoolDays = request.TotalSchoolDays,
				RelocationRequestId = requestId,
				IsMidSemesterChange = true
			};
			await _historyRepo.AddAsync(newAssignment);

			// 4. Update student's current pickup point
			var student = await _studentRepo.FindAsync(request.StudentId);
			if (student != null)
			{
				student.CurrentPickupPointId = newPickupPoint.Id;
				student.PickupPointAssignedAt = DateTime.UtcNow;
				await _studentRepo.UpdateAsync(student);
			}

			// 5. Handle financial transactions
			if (request.AdditionalPaymentRequired > 0)
			{
				// Create transaction for additional payment
				try
				{
					if (student?.ParentId != null)
					{
						var transaction = new Transaction
						{
							ParentId = student.ParentId.Value,
							TransactionCode = $"REL-ADD-{DateTime.UtcNow:yyyyMMddHHmmss}-{request.Id.ToString().Substring(0, 8)}",
							Status = TransactionStatus.Notyet,
							Amount = request.AdditionalPaymentRequired,
							Currency = "VND",
							Description = $"Additional payment for relocation to {request.NewPickupPointAddress}",
							Provider = PaymentProvider.PayOS,
							RelocationRequestId = requestId,
							TransactionType = TransactionTypeConstants.AdditionalPayment
						};

						await _transactionRepo.AddAsync(transaction);
						request.AdditionalPaymentTransactionId = transaction.Id;
						request.RequestStatus = RelocationRequestStatus.AwaitingPayment;
					}
				}
				catch (Exception)
				{
					// Log error but don't fail the approval
					request.RequestStatus = RelocationRequestStatus.Approved;
				}
			}
			else if (request.RefundAmount > 0)
			{
				// Create refund transaction
				try
				{
					if (student?.ParentId != null)
					{
						var refundTransaction = new Transaction
						{
							ParentId = student.ParentId.Value,
							TransactionCode = $"REL-REF-{DateTime.UtcNow:yyyyMMddHHmmss}-{request.Id.ToString().Substring(0, 8)}",
							Status = TransactionStatus.Paid,
							Amount = -request.RefundAmount,
							Currency = "VND",
							Description = $"Refund for relocation from {request.OldPickupPointAddress}",
							Provider = PaymentProvider.PayOS,
							PaidAtUtc = DateTime.UtcNow,
							RelocationRequestId = requestId,
							TransactionType = TransactionTypeConstants.Refund
						};

						await _transactionRepo.AddAsync(refundTransaction);
						request.RefundTransactionId = refundTransaction.Id;
						request.RequestStatus = RelocationRequestStatus.Approved;
					}
				}
				catch (Exception)
				{
					// Log error but don't fail the approval
					request.RequestStatus = RelocationRequestStatus.Approved;
				}
			}
			else
			{
				request.RequestStatus = RelocationRequestStatus.Approved;
			}

			// 6. Update request
			request.NewPickupPointId = newPickupPoint.Id;
			request.ReviewedByAdminId = adminId;
			request.ReviewedAt = DateTime.UtcNow;
			request.AdminNotes = dto.AdminNotes;
			request.AdminDecision = "Approved";
			request.EffectiveDate = effectiveDate;
			request.ImplementedAt = DateTime.UtcNow;

			var updated = await _relocationRepo.UpdateAsync(request);
			return MapToResponseDto(updated);
		}

		public async Task<RelocationRequestResponseDto> RejectRequestAsync(
			Guid requestId,
			RejectRelocationRequestDto dto,
			Guid adminId)
		{
			var request = await _relocationRepo.FindAsync(requestId);
			if (request == null)
				throw new KeyNotFoundException("Relocation request not found.");

			if (request.RequestStatus != RelocationRequestStatus.Pending)
				throw new InvalidOperationException($"Cannot reject request with status: {request.RequestStatus}");

			request.RequestStatus = RelocationRequestStatus.Rejected;
			request.ReviewedByAdminId = adminId;
			request.ReviewedAt = DateTime.UtcNow;
			request.AdminNotes = dto.AdminNotes;
			request.AdminDecision = "Rejected";
			request.RejectionReason = dto.RejectionReason;

			var updated = await _relocationRepo.UpdateAsync(request);
			return MapToResponseDto(updated);
		}

		public async Task<RelocationRequestListResponse> GetMyRequestsAsync(
			Guid parentId,
			string? status = null,
			int page = 1,
			int perPage = 20)
		{
			var skip = (page - 1) * perPage;
			var requests = await _relocationRepo.GetByParentIdAsync(parentId, status, skip, perPage);

			return new RelocationRequestListResponse
			{
				Data = requests.Select(MapToResponseDto).ToList(),
				TotalCount = requests.Count,
				Page = page,
				PerPage = perPage
			};
		}

		public async Task<RelocationRequestListResponse> GetAllRequestsAsync(
			string? status = null,
			string? semesterCode = null,
			DateTime? fromDate = null,
			DateTime? toDate = null,
			int page = 1,
			int perPage = 20)
		{
			var skip = (page - 1) * perPage;
			var requests = await _relocationRepo.GetAllAsync(status, semesterCode, fromDate, toDate, skip, perPage);

			return new RelocationRequestListResponse
			{
				Data = requests.Select(MapToResponseDto).ToList(),
				TotalCount = requests.Count,
				Page = page,
				PerPage = perPage
			};
		}

		public async Task<RelocationRequestResponseDto?> GetRequestByIdAsync(Guid requestId)
		{
			var request = await _relocationRepo.FindAsync(requestId);
			return request == null ? null : MapToResponseDto(request);
		}

		// Helper methods
		private async Task<double> CalculateDistanceFromSchoolAsync(Point? pickupPointGeog)
		{
			if (pickupPointGeog == null)
				return 0;

			try
			{
				// Get school location
				var school = await _schoolService.GetSchoolAsync();

				if (school == null || school.Latitude == null || school.Longitude == null)
				{
					// Fallback: use default distance if school location not configured
					return 5.0;
				}

				// Calculate distance using VietMap service
				var distance = await _vietMapService.CalculateDistanceAsync(
					school.Latitude.Value,
					school.Longitude.Value,
					pickupPointGeog.Y,  // Latitude
					pickupPointGeog.X   // Longitude
				);

				return distance ?? 5.0; // Fallback to 5km if calculation fails
			}
			catch
			{
				// Graceful degradation: return default distance
				return 5.0;
			}
		}

		private async Task<decimal> GetCurrentUnitPriceAsync()
		{
			var unitPrice = await _transactionService.GetCurrentActiveUnitPriceAsync(null);
			return unitPrice.PricePerKm;
		}

		private RelocationRequestResponseDto MapToResponseDto(RelocationRequestDocument doc)
		{
			return new RelocationRequestResponseDto
			{
				Id = doc.Id,
				RequestType = doc.RequestType,
				RequestStatus = doc.RequestStatus,
				Priority = doc.Priority,

				ParentId = doc.ParentId,
				ParentEmail = doc.ParentEmail,
				StudentId = doc.StudentId,
				StudentName = doc.StudentName,

				SemesterCode = doc.SemesterCode,
				SemesterName = doc.SemesterName,
				AcademicYear = doc.AcademicYear,
				TotalSchoolDays = doc.TotalSchoolDays,
				DaysServiced = doc.DaysServiced,
				DaysRemaining = doc.DaysRemaining,

				OldPickupPointId = doc.OldPickupPointId,
				OldPickupPointAddress = doc.OldPickupPointAddress,
				OldDistanceKm = doc.OldDistanceKm,

				NewPickupPointAddress = doc.NewPickupPointAddress,
				NewLatitude = doc.NewLatitude,
				NewLongitude = doc.NewLongitude,
				NewDistanceKm = doc.NewDistanceKm,
				NewPickupPointId = doc.NewPickupPointId,
				IsOnExistingRoute = doc.IsOnExistingRoute,

				OriginalPaymentAmount = doc.OriginalPaymentAmount,
				ValueServiced = doc.ValueServiced,
				ValueRemaining = doc.ValueRemaining,
				NewLocationCost = doc.NewLocationCost,
				RefundAmount = doc.RefundAmount,
				AdditionalPaymentRequired = doc.AdditionalPaymentRequired,
				ProcessingFee = doc.ProcessingFee,
				UnitPricePerKm = doc.UnitPricePerKm,

				Reason = doc.Reason,
				Description = doc.Description,
				EvidenceUrls = doc.EvidenceUrls,
				UrgentRequest = doc.UrgentRequest,
				RequestedEffectiveDate = doc.RequestedEffectiveDate,

				AIRecommendation = doc.AIRecommendation == null ? null : new AIRecommendationDto
				{
					Recommendation = doc.AIRecommendation.Recommendation,
					Confidence = doc.AIRecommendation.Confidence,
					Score = doc.AIRecommendation.Score,
					Summary = doc.AIRecommendation.Summary,
					Reasons = doc.AIRecommendation.Reasons,
					SuggestedActions = doc.AIRecommendation.SuggestedActions,
					RiskFactors = doc.AIRecommendation.RiskFactors,
					CalculatedAt = doc.AIRecommendation.CalculatedAt
				},

				ReviewedByAdminId = doc.ReviewedByAdminId,
				ReviewedByAdminName = doc.ReviewedByAdminName,
				ReviewedAt = doc.ReviewedAt,
				AdminNotes = doc.AdminNotes,
				AdminDecision = doc.AdminDecision,
				RejectionReason = doc.RejectionReason,

				ImplementedAt = doc.ImplementedAt,
				EffectiveDate = doc.EffectiveDate,

				SubmittedAt = doc.SubmittedAt,
				LastStatusUpdate = doc.LastStatusUpdate,
				CreatedAt = doc.CreatedAt
			};
		}
	}
}