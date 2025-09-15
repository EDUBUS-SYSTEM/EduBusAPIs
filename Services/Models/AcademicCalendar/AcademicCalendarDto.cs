namespace Services.Models.AcademicCalendar
{
    public class AcademicCalendarDto
    {
        public Guid Id { get; set; }
        public string AcademicYear { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<AcademicSemesterDto> Semesters { get; set; } = new List<AcademicSemesterDto>();
        public List<SchoolHolidayDto> Holidays { get; set; } = new List<SchoolHolidayDto>();
        public List<SchoolDayDto> SchoolDays { get; set; } = new List<SchoolDayDto>();
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class AcademicSemesterDto
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class SchoolHolidayDto
    {
        public string Name { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsRecurring { get; set; }
    }

    public class SchoolDayDto
    {
        public DateTime Date { get; set; }
        public bool IsSchoolDay { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class AcademicCalendarCreateDto
    {
        public string AcademicYear { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<AcademicSemesterDto> Semesters { get; set; } = new List<AcademicSemesterDto>();
        public List<SchoolHolidayDto> Holidays { get; set; } = new List<SchoolHolidayDto>();
        public List<SchoolDayDto> SchoolDays { get; set; } = new List<SchoolDayDto>();
        public bool IsActive { get; set; } = true;
    }

    public class AcademicCalendarUpdateDto
    {
        public string AcademicYear { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<AcademicSemesterDto> Semesters { get; set; } = new List<AcademicSemesterDto>();
        public List<SchoolHolidayDto> Holidays { get; set; } = new List<SchoolHolidayDto>();
        public List<SchoolDayDto> SchoolDays { get; set; } = new List<SchoolDayDto>();
        public bool IsActive { get; set; }
    }
}
