using System.ComponentModel.DataAnnotations;

namespace Services.Models.StudentGrade
{
    public class CreateStudentGradeRequest
    {
        [Required(ErrorMessage = "StudentId is required.")]
        public Guid StudentId { get; set; }

        [Required(ErrorMessage = "GradeId is required.")]
        public Guid GradeId { get; set; }

        [Required(ErrorMessage = "Start time is required.")]
        public DateTime StartTimeUtc { get; set; }

        [Required(ErrorMessage = "End time is required.")]
        public DateTime EndTimeUtc { get; set; }
    }
}
