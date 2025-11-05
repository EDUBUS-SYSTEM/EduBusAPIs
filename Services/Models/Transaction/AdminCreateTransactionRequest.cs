using System.ComponentModel.DataAnnotations;

namespace Services.Models.Transaction
{
    public class AdminCreateTransactionRequest
    {
        [Required(ErrorMessage = "Parent ID is required")]
        public Guid ParentId { get; set; }

        [Required(ErrorMessage = "Student IDs are required")]
        [MinLength(1, ErrorMessage = "At least one student must be specified")]
        public List<Guid> StudentIds { get; set; } = new();

        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; } = null!;

        [Required(ErrorMessage = "Due date is required")]
        public DateTime DueDate { get; set; }

        [Required(ErrorMessage = "Distance is required")]
        [Range(0.01, 1000, ErrorMessage = "Distance must be between 0.01 and 1000 km")]
        public double DistanceKm { get; set; }

        [Required(ErrorMessage = "Unit price per kilometer is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than 0")]
        public decimal UnitPricePerKm { get; set; }

        /// <summary>
        /// Metadata for transaction tracking (JSON)
        /// </summary>
        public TransactionMetadata? Metadata { get; set; }
    }

    public class TransactionMetadata
    {
        public double DistanceKm { get; set; }
        public decimal PerTripFee { get; set; }
        public int TotalSchoolDays { get; set; }
        public int TotalTrips { get; set; }
        public string? SemesterName { get; set; }
        public string? AcademicYear { get; set; }
    }
}
