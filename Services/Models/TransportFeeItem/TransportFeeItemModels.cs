using System.ComponentModel.DataAnnotations;
using Data.Models.Enums;

namespace Services.Models.TransportFeeItem
{
    public class TransportFeeItemListRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        
        // Filters
        public Guid? TransactionId { get; set; }
        public Guid? StudentId { get; set; }
        public string? ParentEmail { get; set; }
        public TransportFeeItemStatus? Status { get; set; }
        public string? SemesterName { get; set; }
        public string? AcademicYear { get; set; }
        public TransportFeeItemType? Type { get; set; }
    }

    public class TransportFeeItemListResponse
    {
        public List<Data.Models.TransportFeeItem> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class TransportFeeItemDetailResponse
    {
        public Guid Id { get; set; }
        public Guid TransactionId { get; set; }
        public string TransactionCode { get; set; } = string.Empty;
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string ParentEmail { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double DistanceKm { get; set; }
        public decimal UnitPricePerKm { get; set; }
        public decimal Subtotal { get; set; }
        public string SemesterName { get; set; } = string.Empty;
        public string AcademicYear { get; set; } = string.Empty;
        public DateTime SemesterStartDate { get; set; }
        public DateTime SemesterEndDate { get; set; }
        public int TotalSchoolDays { get; set; }
        public TransportFeeItemStatus Status { get; set; }
        public TransportFeeItemType Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateTransportFeeItemRequest
    {
        [Required]
        public Guid TransactionId { get; set; }
        
        [Required]
        public Guid StudentId { get; set; }
        
        [Required]
        [EmailAddress]
        [MaxLength(320)]
        public string ParentEmail { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;
        
        [Range(0.01, 1000, ErrorMessage = "Distance must be between 0.01 and 1000 km")]
        public double DistanceKm { get; set; }
        
        [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than 0")]
        public decimal UnitPricePerKm { get; set; }
        
        [Range(0.01, double.MaxValue, ErrorMessage = "Subtotal must be greater than 0")]
        public decimal Subtotal { get; set; }
        
        [Required]
        public Guid UnitPriceId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string SemesterName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(20)]
        public string AcademicYear { get; set; } = string.Empty;
        
        [Required]
        public DateTime SemesterStartDate { get; set; }
        
        [Required]
        public DateTime SemesterEndDate { get; set; }
        
        [Range(1, 365, ErrorMessage = "Total school days must be between 1 and 365")]
        public int TotalSchoolDays { get; set; }
        
        public TransportFeeItemType Type { get; set; } = TransportFeeItemType.Register;
    }

    public class UpdateTransportFeeItemStatusRequest
    {
        [Required]
        public Guid Id { get; set; }
        
        [Required]
        public TransportFeeItemStatus Status { get; set; }
        
        [MaxLength(500)]
        public string? Notes { get; set; }
    }

    public class TransportFeeItemSummary
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Subtotal { get; set; }
        public TransportFeeItemStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
