using Data.Models;

public class PickupPointRequestDocument : BaseMongoDocument
{
    public Guid? ParentId { get; set; }
    public string ParentEmail { get; set; } = "";
    public List<Guid> StudentIds { get; set; } = new();

    public string AddressText { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double DistanceKm { get; set; }

    public string Description { get; set; } = "";
    public string Reason { get; set; } = "";

    public string Status { get; set; } = "Pending";   // Pending, Approved, Rejected
    public string AdminNotes { get; set; } = "";
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedByAdminId { get; set; }
    public Guid? PickupPointId { get; set; }  // ID of created pickup point

    // Pricing information (snapshot at submission time)
    public decimal UnitPricePerKm { get; set; }
    public decimal TotalFee { get; set; }
    
    // Semester information (snapshot at submission time)
    public string SemesterName { get; set; } = "";
    public string AcademicYear { get; set; } = "";
    public DateTime SemesterStartDate { get; set; }
    public DateTime SemesterEndDate { get; set; }
    public int TotalSchoolDays { get; set; }
}
