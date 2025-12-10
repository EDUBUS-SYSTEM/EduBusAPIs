using Data.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Data.Models
{
	public class RelocationRequestDocument : BaseMongoDocument
	{
		// Request Metadata
		[BsonElement("requestType")]
		public string RequestType { get; set; } = RelocationRequestType.Standard;

		[BsonElement("requestStatus")]
		public string RequestStatus { get; set; } = RelocationRequestStatus.Pending;

		[BsonElement("priority")]
		public string Priority { get; set; } = "Normal"; // Low, Normal, High, Urgent

		// Parent & Student Info
		[BsonElement("parentId")]
		public Guid ParentId { get; set; }

		[BsonElement("parentEmail")]
		public string ParentEmail { get; set; } = string.Empty;

		[BsonElement("studentId")]
		public Guid StudentId { get; set; }

		[BsonElement("studentName")]
		public string StudentName { get; set; } = string.Empty;

		// Semester Context
		[BsonElement("semesterCode")]
		public string SemesterCode { get; set; } = string.Empty;

		[BsonElement("semesterName")]
		public string SemesterName { get; set; } = string.Empty;

		[BsonElement("academicYear")]
		public string AcademicYear { get; set; } = string.Empty;

		[BsonElement("totalSchoolDays")]
		public int TotalSchoolDays { get; set; }

		[BsonElement("daysServiced")]
		public int DaysServiced { get; set; }

		[BsonElement("daysRemaining")]
		public int DaysRemaining { get; set; }

		// OLD Location
		[BsonElement("oldPickupPointId")]
		public Guid OldPickupPointId { get; set; }

		[BsonElement("oldPickupPointAddress")]
		public string OldPickupPointAddress { get; set; } = string.Empty;

		[BsonElement("oldDistanceKm")]
		public double OldDistanceKm { get; set; }

		[BsonElement("oldRouteId")]
		public Guid? OldRouteId { get; set; }

		// NEW Location
		[BsonElement("newPickupPointAddress")]
		public string NewPickupPointAddress { get; set; } = string.Empty;

		[BsonElement("newLatitude")]
		public double NewLatitude { get; set; }

		[BsonElement("newLongitude")]
		public double NewLongitude { get; set; }

		[BsonElement("newDistanceKm")]
		public double NewDistanceKm { get; set; }

		[BsonElement("newPickupPointId")]
		public Guid? NewPickupPointId { get; set; } // Set after approval

		[BsonElement("newRouteId")]
		public Guid? NewRouteId { get; set; }

		[BsonElement("isOnExistingRoute")]
		public bool IsOnExistingRoute { get; set; }

		// Financial Impact
		[BsonElement("originalPaymentAmount")]
		[BsonRepresentation(BsonType.Decimal128)]
		public decimal OriginalPaymentAmount { get; set; }

		[BsonElement("valueServiced")]
		[BsonRepresentation(BsonType.Decimal128)]
		public decimal ValueServiced { get; set; }

		[BsonElement("valueRemaining")]
		[BsonRepresentation(BsonType.Decimal128)]
		public decimal ValueRemaining { get; set; }

		[BsonElement("newLocationCost")]
		[BsonRepresentation(BsonType.Decimal128)]
		public decimal NewLocationCost { get; set; }

		[BsonElement("refundAmount")]
		[BsonRepresentation(BsonType.Decimal128)]
		public decimal RefundAmount { get; set; }

		[BsonElement("additionalPaymentRequired")]
		[BsonRepresentation(BsonType.Decimal128)]
		public decimal AdditionalPaymentRequired { get; set; }

		[BsonElement("processingFee")]
		[BsonRepresentation(BsonType.Decimal128)]
		public decimal ProcessingFee { get; set; }

		[BsonElement("unitPricePerKm")]
		[BsonRepresentation(BsonType.Decimal128)]
		public decimal UnitPricePerKm { get; set; }

		// Request Details
		[BsonElement("reason")]
		public string Reason { get; set; } = string.Empty; // Enum: FamilyRelocation, Medical, etc.

		[BsonElement("description")]
		public string Description { get; set; } = string.Empty;

		[BsonElement("evidenceUrls")]
		public List<string> EvidenceUrls { get; set; } = new();

		[BsonElement("urgentRequest")]
		public bool UrgentRequest { get; set; }

		[BsonElement("requestedEffectiveDate")]
		public DateTime RequestedEffectiveDate { get; set; }

		// AI Recommendation (populated by system)
		[BsonElement("aiRecommendation")]
		public AIRecommendation? AIRecommendation { get; set; }

		// Admin Review
		[BsonElement("reviewedByAdminId")]
		public Guid? ReviewedByAdminId { get; set; }

		[BsonElement("reviewedByAdminName")]
		public string? ReviewedByAdminName { get; set; }

		[BsonElement("reviewedAt")]
		public DateTime? ReviewedAt { get; set; }

		[BsonElement("adminNotes")]
		public string AdminNotes { get; set; } = string.Empty;

		[BsonElement("adminDecision")]
		public string? AdminDecision { get; set; } // Approved, Rejected, Conditional

		[BsonElement("rejectionReason")]
		public string? RejectionReason { get; set; }

		// Implementation
		[BsonElement("implementedAt")]
		public DateTime? ImplementedAt { get; set; }

		[BsonElement("effectiveDate")]
		public DateTime? EffectiveDate { get; set; } // When the change takes effect

		[BsonElement("refundTransactionId")]
		public Guid? RefundTransactionId { get; set; }

		[BsonElement("additionalPaymentTransactionId")]
		public Guid? AdditionalPaymentTransactionId { get; set; }

		// Tracking
		[BsonElement("submittedAt")]
		public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

		[BsonElement("lastStatusUpdate")]
		public DateTime LastStatusUpdate { get; set; } = DateTime.UtcNow;
	}

	public class AIRecommendation
	{
		[BsonElement("recommendation")]
		public string Recommendation { get; set; } = string.Empty; // APPROVE, REJECT, REVIEW

		[BsonElement("confidence")]
		public string Confidence { get; set; } = string.Empty; // High, Medium, Low

		[BsonElement("score")]
		public int Score { get; set; } // 0-100

		[BsonElement("summary")]
		public string Summary { get; set; } = string.Empty;

		[BsonElement("reasons")]
		public List<string> Reasons { get; set; } = new();

		[BsonElement("suggestedActions")]
		public List<string> SuggestedActions { get; set; } = new();

		[BsonElement("riskFactors")]
		public List<string> RiskFactors { get; set; } = new();

		[BsonElement("calculatedAt")]
		public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
	}

	public static class RelocationRequestType
	{
		public const string Standard = "Standard"; // Normal relocation
		public const string Emergency = "Emergency"; // Urgent/medical
		public const string Cancellation = "Cancellation"; // Full withdrawal
		public const string Multiple = "Multiple"; // Multiple students
	}

	public static class RelocationRequestStatus
	{
		public const string Draft = "Draft";
		public const string Pending = "Pending";
		public const string UnderReview = "UnderReview";
		public const string AwaitingPayment = "AwaitingPayment";
		public const string Approved = "Approved";
		public const string Rejected = "Rejected";
		public const string Implemented = "Implemented";
		public const string Cancelled = "Cancelled";
	}

	public static class RefundReason
	{
		public const string FamilyRelocation = "FamilyRelocation";
		public const string Medical = "Medical";
		public const string Safety = "Safety";
		public const string FamilyEmergency = "FamilyEmergency";
		public const string ServiceQuality = "ServiceQuality";
		public const string Financial = "Financial";
		public const string Convenience = "Convenience";
		public const string Other = "Other";
	}
}