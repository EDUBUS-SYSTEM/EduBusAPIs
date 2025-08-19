namespace Data.Models;

public partial class Student : BaseDomain
{
    public Guid ParentId { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<Image> Images { get; set; } = new List<Image>();

    public virtual Parent Parent { get; set; } = null!;

    public virtual ICollection<StudentGradeEnrollment> StudentGradeEnrollments { get; set; } = new List<StudentGradeEnrollment>();

    public virtual StudentPickupPoint? StudentPickupPoint { get; set; }

    public virtual ICollection<TransportFeeItem> TransportFeeItems { get; set; } = new List<TransportFeeItem>();
}
