namespace Services.Models.PickupPoint
{
    public class SubmitPickupPointRequestResponseDto
    {
        public Guid RequestId { get; set; }
        public string Status { get; set; } = "Pending";
        public string Message { get; set; } = string.Empty;
        public decimal TotalFee { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Additional information for UI display
        public string SemesterName { get; set; } = string.Empty;
        public string AcademicYear { get; set; } = string.Empty;
        public int TotalSchoolDays { get; set; }
        public string CalculationDetails { get; set; } = string.Empty;
    }
}
