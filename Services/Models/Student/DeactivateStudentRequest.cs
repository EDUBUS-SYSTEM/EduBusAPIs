using System.ComponentModel.DataAnnotations;

namespace Services.Models.Student
{
    public class DeactivateStudentRequest
    {
        [Required(ErrorMessage = "Deactivation reason is required.")]
        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
        public string Reason { get; set; } = string.Empty;
    }
}
