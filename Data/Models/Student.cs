namespace Data.Models;

public partial class Student : BaseDomain
{
    public Guid? ParentId { get; set; }

    public string ParentPhoneNumber { get; set; } = string.Empty;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public bool IsActive { get; set; }

    public Guid? CurrentPickupPointId { get; set; }
    public DateTime? PickupPointAssignedAt { get; set; }

    public virtual ICollection<Image> Images { get; set; } = new List<Image>();

    public virtual Parent? Parent { get; set; }

    public virtual ICollection<StudentGradeEnrollment> StudentGradeEnrollments { get; set; } = new List<StudentGradeEnrollment>();

    public virtual PickupPoint? CurrentPickupPoint { get; set; }

    public virtual ICollection<StudentPickupPointHistory> PickupPointHistory { get; set; } = new List<StudentPickupPointHistory>();

    public virtual ICollection<TransportFeeItem> TransportFeeItems { get; set; } = new List<TransportFeeItem>();
}
