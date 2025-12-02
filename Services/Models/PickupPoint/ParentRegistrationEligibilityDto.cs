using System;
using System.Collections.Generic;

namespace Services.Models.PickupPoint
{
    public class ParentRegistrationEligibilityDto
    {
        public bool IsRegistrationWindowOpen { get; set; }
        public bool HasEligibleStudents => EligibleStudents.Count > 0;
        public ParentRegistrationSemesterDto? Semester { get; set; }
        public List<StudentBriefDto> EligibleStudents { get; set; } = new();
        public List<StudentRegistrationBlockDto> BlockedStudents { get; set; } = new();
        public string? Message { get; set; }
    }

    public class ParentRegistrationSemesterDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string AcademicYear { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? RegistrationStartDate { get; set; }
        public DateTime? RegistrationEndDate { get; set; }
    }

    public class StudentRegistrationBlockDto : StudentBriefDto
    {
        public string Status { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}


