namespace Data.Models;

public partial class StudentGradeEnrollment : BaseDomain
{
    public Guid StudentId { get; set; }

    public Guid GradeId { get; set; }

    public DateTime StartTimeUtc { get; set; }

    public DateTime? EndTimeUtc { get; set; }

    public virtual Grade Grade { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
