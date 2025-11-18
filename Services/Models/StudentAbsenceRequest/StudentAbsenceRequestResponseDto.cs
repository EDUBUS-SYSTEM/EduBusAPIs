using Data.Models.Enums;

namespace Services.Models.StudentAbsenceRequest
{
    public sealed class StudentAbsenceRequestResponseDto
    {
        public Guid Id { get; init; }
        public Guid StudentId { get; init; }
        public Guid ParentId { get; init; }
        public DateTime StartDate { get; init; }
        public DateTime EndDate { get; init; }
        public string Reason { get; init; } = string.Empty;
        public string? Notes { get; init; }
        public AbsenceRequestStatus Status { get; init; }
        public Guid? ReviewedBy { get; init; }
        public DateTime? ReviewedAt { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
    }
}

