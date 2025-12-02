namespace Data.Models;

public partial class StudentPickupPoint : BaseDomain
{
    public Guid StudentId { get; set; }
    public Guid PickupPointId { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? RemovedAt { get; set; }
    public string ChangeReason { get; set; } = string.Empty;
    public string ChangedBy { get; set; } = string.Empty;
    public string SemesterName { get; set; } = string.Empty;
    public string SemesterCode { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public DateTime SemesterStartDate { get; set; }
    public DateTime SemesterEndDate { get; set; }
    public int TotalSchoolDays { get; set; }
    
    public virtual Student Student { get; set; } = null!;
    public virtual PickupPoint PickupPoint { get; set; } = null!;
}
