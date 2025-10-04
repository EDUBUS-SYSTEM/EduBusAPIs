using Data.Models;
using Data.Models.Enums;

namespace Services.Models.PickupPoint
{
    public class PickupPointWithStudentStatusDto
    {
        public Guid Id { get; set; }
        public string Description { get; set; } = null!;
        public string Location { get; set; } = null!;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public int AssignedStudentCount { get; set; }
        public List<StudentInfo> AssignedStudents { get; set; } = new();
    }

    public class StudentInfo
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string FullName => $"{FirstName} {LastName}";
        public StudentStatus Status { get; set; }
        public DateTime? PickupPointAssignedAt { get; set; }
    }
}
