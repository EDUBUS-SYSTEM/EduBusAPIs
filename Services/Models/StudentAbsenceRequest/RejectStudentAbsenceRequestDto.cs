using System.ComponentModel.DataAnnotations;

namespace Services.Models.StudentAbsenceRequest
{
    public sealed class RejectStudentAbsenceRequestDto
    {
        [Required(ErrorMessage = "Rejection reason is required.")]
        [StringLength(500, ErrorMessage = "Rejection reason cannot exceed 500 characters.")]
        public string Reason { get; set; } = string.Empty;
    }
}


