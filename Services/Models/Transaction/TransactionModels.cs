using Data.Models;
using Data.Models.Enums;
using Services.Models.Transaction;
using System.ComponentModel.DataAnnotations;

namespace Services.Models.Transaction
{
    public class CreateTransactionFromPickupPointRequest
    {
        [Required(ErrorMessage = "Pickup point request ID is required")]
        public Guid PickupPointRequestId { get; set; }
        
        [Required(ErrorMessage = "Parent ID is required")]
        public Guid ParentId { get; set; }
        
        [Required(ErrorMessage = "Parent email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [MaxLength(320, ErrorMessage = "Email cannot exceed 320 characters")]
        public string ParentEmail { get; set; } = null!;
        
        [Required(ErrorMessage = "Student IDs are required")]
        [MinLength(1, ErrorMessage = "At least one student must be specified")]
        public List<Guid> StudentIds { get; set; } = new();
        
        [Required(ErrorMessage = "Distance is required")]
        [Range(0.01, 1000, ErrorMessage = "Distance must be between 0.01 and 1000 km")]
        public double DistanceKm { get; set; }
        
        [Required(ErrorMessage = "Unit price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than 0")]
        public decimal UnitPricePerKm { get; set; }
        
        [Required(ErrorMessage = "Total fee is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Total fee must be greater than 0")]
        public decimal TotalFee { get; set; } // Use the pre-calculated fee from PickupPointRequest
        
        [Required(ErrorMessage = "Unit price ID is required")]
        public Guid UnitPriceId { get; set; }
        
        [Required(ErrorMessage = "Approved by admin ID is required")]
        public Guid ApprovedByAdminId { get; set; }
        
        [MaxLength(500, ErrorMessage = "Admin notes cannot exceed 500 characters")]
        public string AdminNotes { get; set; } = string.Empty;
    }

    public class CreateTransactionFromPickupPointResponse
    {
        public List<TransactionInfo> Transactions { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public List<TransportFeeItemInfo> TransportFeeItems { get; set; } = new();
        public string Message { get; set; } = null!;
    }

    public class TransactionInfo
    {
        public Guid TransactionId { get; set; }
        public string TransactionCode { get; set; } = null!;
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Description { get; set; } = null!;
    }

    public class TransportFeeItemInfo
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Description { get; set; } = null!;
    }

    public class TransactionDetailResponseDto
    {
        public Guid Id { get; set; }
        public Guid ParentId { get; set; }
        public string ParentEmail { get; set; } = null!;
        public string TransactionCode { get; set; } = null!;
        public TransactionStatus Status { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = null!;
        public string Description { get; set; } = null!;
        public PaymentProvider Provider { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public List<TransportFeeItemDetail> TransportFeeItems { get; set; } = new();
    }

    public class TransportFeeItemDetail
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = null!;
        public string Description { get; set; } = null!;
        public double DistanceKm { get; set; }
        public decimal UnitPricePerKm { get; set; }
        public decimal Amount { get; set; }
        public string SemesterName { get; set; } = null!;
        public string AcademicYear { get; set; } = null!;
        public TransportFeeItemType Type { get; set; }
        public TransportFeeItemStatus Status { get; set; }
    }

    public class TransactionListRequest
    {
        public Guid? ParentId { get; set; }
        public TransactionStatus? Status { get; set; }
        public string? TransactionCode { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        
        [Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1")]
        public int Page { get; set; } = 1;
        
        [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
        public int PageSize { get; set; } = 20;
    }

    public class TransactionListResponseDto
    {
        public List<TransactionSummary> Transactions { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    public class TransactionSummary
    {
        public Guid Id { get; set; }
        public string TransactionCode { get; set; } = null!;
        public TransactionStatus Status { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = null!;
        public string Description { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAtUtc { get; set; }
        public Guid ParentId { get; set; }
        public int StudentCount { get; set; }
    }

    public class AcademicSemesterInfo
    {
        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string AcademicYear { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<DateTime> Holidays { get; set; } = new();
        public int TotalSchoolDays { get; set; }
        public int TotalTrips { get; set; } // Total school days * 2 (round trip)
    }

    public class CalculateFeeRequest
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Distance must be greater than 0")]
        public double DistanceKm { get; set; }
        
        // Optional: If not provided, will use current active unit price
        public Guid? UnitPriceId { get; set; }
        
        /// <summary>
        /// Số lượng học sinh (để tính chính sách giảm giá)
        /// </summary>
        public int StudentCount { get; set; } = 1;
    }

    public class CalculateFeeResponse
    {
        public decimal TotalFee { get; set; }
        public decimal OriginalFee { get; set; } // Phí gốc trước khi áp dụng chính sách
        public decimal PolicyReductionAmount { get; set; } // Số tiền được giảm
        public decimal PolicyReductionPercentage { get; set; } // % giảm giá
        public string PolicyDescription { get; set; } = string.Empty; // Mô tả chính sách
        public decimal UnitPricePerKm { get; set; }
        public double DistanceKm { get; set; }
        public int TotalSchoolDays { get; set; }
        public int TotalTrips { get; set; }
        public double TotalDistanceKm { get; set; }
        public int StudentCount { get; set; } // Số lượng học sinh
        public string SemesterName { get; set; } = null!;
        public string AcademicYear { get; set; } = null!;
        public DateTime SemesterStartDate { get; set; }
        public DateTime SemesterEndDate { get; set; }
        public List<DateTime> Holidays { get; set; } = new();
        public string CalculationDetails { get; set; } = null!;
    }
}

