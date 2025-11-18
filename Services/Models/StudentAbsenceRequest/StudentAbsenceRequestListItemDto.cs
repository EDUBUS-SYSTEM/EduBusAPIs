using Data.Models.Enums;

namespace Services.Models.StudentAbsenceRequest
{
    public sealed class StudentAbsenceRequestListItemDto
    {
        public Guid Id { get; init; }
        public Guid StudentId { get; init; }
        public string StudentName { get; init; } = string.Empty;
        public DateTime StartDate { get; init; }
        public DateTime EndDate { get; init; }
        public string Reason { get; init; } = string.Empty;
        public AbsenceRequestStatus Status { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
    }
}

