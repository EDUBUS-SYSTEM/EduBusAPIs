using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data.Models.Enums;

namespace Services.Models.Student
{
    public class StudentDto
    {
        public Guid Id { get; set; }
        public Guid? ParentId { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string ParentEmail { get; set; } = string.Empty;
        public StudentStatus Status { get; set; } = StudentStatus.Available;
        public DateTime? ActivatedAt { get; set; }
        public DateTime? DeactivatedAt { get; set; }
        public string? DeactivationReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? StudentImageId { get; set; }
    }
}
