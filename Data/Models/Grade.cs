namespace Data.Models;

public partial class Grade : BaseDomain
{
    public string Name { get; set; } = null!;

    public virtual ICollection<StudentGradeEnrollment> StudentGradeEnrollments { get; set; } = new List<StudentGradeEnrollment>();
}
