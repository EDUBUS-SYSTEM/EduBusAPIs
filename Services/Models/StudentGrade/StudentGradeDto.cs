namespace Services.Models.StudentGrade
{
    public class StudentGradeDto
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public Guid GradeId { get; set; }
        public DateTime StartTimeUtc { get; set; }
        public DateTime EndTimeUtc { get; set; }

    }
}
