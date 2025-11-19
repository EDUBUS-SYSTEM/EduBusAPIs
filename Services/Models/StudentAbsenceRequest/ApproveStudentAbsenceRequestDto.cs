using System.ComponentModel.DataAnnotations;

namespace Services.Models.StudentAbsenceRequest
{
    public sealed class ApproveStudentAbsenceRequestDto
    {
        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
        public string? Notes { get; init; }
    }
}


