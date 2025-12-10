namespace Services.Models.RelocationRequest
{
	public class CreateRelocationRequestDto
	{
		public Guid StudentId { get; set; }
		public string NewPickupPointAddress { get; set; } = string.Empty;
		public double NewLatitude { get; set; }
		public double NewLongitude { get; set; }
		public double NewDistanceKm { get; set; }
		public string Reason { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
		public List<string> EvidenceUrls { get; set; } = new();
		public bool UrgentRequest { get; set; }
		public DateTime RequestedEffectiveDate { get; set; }
	}

	public class ApproveRelocationRequestDto
	{
		public string AdminNotes { get; set; } = string.Empty;
		public DateTime? EffectiveDate { get; set; }
	}

	public class RejectRelocationRequestDto
	{
		public string RejectionReason { get; set; } = string.Empty;
		public string AdminNotes { get; set; } = string.Empty;
	}

	// Response DTOs
	public class RelocationRequestResponseDto
	{
		public Guid Id { get; set; }
		public string RequestType { get; set; } = string.Empty;
		public string RequestStatus { get; set; } = string.Empty;
		public string Priority { get; set; } = string.Empty;

		// Student & Parent
		public Guid ParentId { get; set; }
		public string ParentEmail { get; set; } = string.Empty;
		public Guid StudentId { get; set; }
		public string StudentName { get; set; } = string.Empty;

		// Semester
		public string SemesterCode { get; set; } = string.Empty;
		public string SemesterName { get; set; } = string.Empty;
		public string AcademicYear { get; set; } = string.Empty;
		public int TotalSchoolDays { get; set; }
		public int DaysServiced { get; set; }
		public int DaysRemaining { get; set; }

		// Old Location
		public Guid OldPickupPointId { get; set; }
		public string OldPickupPointAddress { get; set; } = string.Empty;
		public double OldDistanceKm { get; set; }

		// New Location
		public string NewPickupPointAddress { get; set; } = string.Empty;
		public double NewLatitude { get; set; }
		public double NewLongitude { get; set; }
		public double NewDistanceKm { get; set; }
		public Guid? NewPickupPointId { get; set; }
		public bool IsOnExistingRoute { get; set; }

		// Financial
		public decimal OriginalPaymentAmount { get; set; }
		public decimal ValueServiced { get; set; }
		public decimal ValueRemaining { get; set; }
		public decimal NewLocationCost { get; set; }
		public decimal RefundAmount { get; set; }
		public decimal AdditionalPaymentRequired { get; set; }
		public decimal ProcessingFee { get; set; }
		public decimal UnitPricePerKm { get; set; }

		// Request Details
		public string Reason { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
		public List<string> EvidenceUrls { get; set; } = new();
		public bool UrgentRequest { get; set; }
		public DateTime RequestedEffectiveDate { get; set; }

		// AI Recommendation
		public AIRecommendationDto? AIRecommendation { get; set; }

		// Admin Review
		public Guid? ReviewedByAdminId { get; set; }
		public string? ReviewedByAdminName { get; set; }
		public DateTime? ReviewedAt { get; set; }
		public string AdminNotes { get; set; } = string.Empty;
		public string? AdminDecision { get; set; }
		public string? RejectionReason { get; set; }

		// Implementation
		public DateTime? ImplementedAt { get; set; }
		public DateTime? EffectiveDate { get; set; }

		// Tracking
		public DateTime SubmittedAt { get; set; }
		public DateTime LastStatusUpdate { get; set; }
		public DateTime CreatedAt { get; set; }
	}

	public class AIRecommendationDto
	{
		public string Recommendation { get; set; } = string.Empty;
		public string Confidence { get; set; } = string.Empty;
		public int Score { get; set; }
		public string Summary { get; set; } = string.Empty;
		public List<string> Reasons { get; set; } = new();
		public List<string> SuggestedActions { get; set; } = new();
		public List<string> RiskFactors { get; set; } = new();
		public DateTime CalculatedAt { get; set; }
	}

	public class RefundCalculationResult
	{
		public decimal OriginalPayment { get; set; }
		public int TotalSchoolDays { get; set; }
		public int DaysServiced { get; set; }
		public int DaysRemaining { get; set; }
		public decimal ValueServiced { get; set; }
		public decimal ValueRemaining { get; set; }
		public decimal RefundPercentage { get; set; }
		public decimal GrossRefund { get; set; }
		public decimal ProcessingFee { get; set; }
		public decimal NetRefund { get; set; }
		public string Reason { get; set; } = string.Empty;
		public decimal NewLocationCost { get; set; }
		public decimal AdditionalPaymentRequired { get; set; }
	}

	public class RelocationRequestListResponse
	{
		public List<RelocationRequestResponseDto> Data { get; set; } = new();
		public int TotalCount { get; set; }
		public int Page { get; set; }
		public int PerPage { get; set; }
	}
}