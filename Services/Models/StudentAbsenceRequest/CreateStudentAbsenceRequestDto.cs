using Data.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Services.Models.StudentAbsenceRequest
{
    public sealed class CreateStudentAbsenceRequestDto 
    {
        [Required(ErrorMessage = "StudentId is required.")]
        public Guid StudentId { get; set; }

        public Guid ParentId { get; set; } =new Guid();

        [Required(ErrorMessage = "Start date is required.")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required.")]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "Reason is required.")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "Reason must be between 5 and 500 characters.")]
        public string Reason { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters.")]
        public string? Notes { get; set; }
    }
    public sealed class UpdateStudentAbsenceStatusDto
    {
        [Required(ErrorMessage = "RequestId is required.")]
        public Guid RequestId { get; set; }
        [Required(ErrorMessage = "Status is required.")]
        public AbsenceRequestStatus Status { get; set; }
        public string? Notes { get; set; }
        public Guid? ReviewedBy { get; set; }
    }
}
