using System;

namespace Services.Models.Trip
{
    public class ParentStudentAssignment
    {
        public Guid ParentId { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
    }
}

