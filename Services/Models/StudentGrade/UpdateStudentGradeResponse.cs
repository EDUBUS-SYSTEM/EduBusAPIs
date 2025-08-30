using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Models.StudentGrade
{
    public class UpdateStudentGradeResponse
    {
        [Required(ErrorMessage = "StudentGradeId is required.")]
        public Guid Id { get; set; }
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
