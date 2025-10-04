using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Data.Models
{
    public class AcademicCalendar : BaseMongoDocument
    {
        [BsonElement("academicYear")]
        public string AcademicYear { get; set; } = string.Empty;

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("startDate")]
        public DateTime StartDate { get; set; }

        [BsonElement("endDate")]
        public DateTime EndDate { get; set; }

        [BsonElement("semesters")]
        public List<AcademicSemester> Semesters { get; set; } = new List<AcademicSemester>();

        [BsonElement("holidays")]
        public List<SchoolHoliday> Holidays { get; set; } = new List<SchoolHoliday>();

        [BsonElement("schoolDays")]
        public List<SchoolDay> SchoolDays { get; set; } = new List<SchoolDay>();

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;
    }

    public class AcademicSemester
    {
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("code")]
        public string Code { get; set; } = string.Empty;

        [BsonElement("startDate")]
        public DateTime StartDate { get; set; }

        [BsonElement("endDate")]
        public DateTime EndDate { get; set; }

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;
    }

    public class SchoolHoliday
    {
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("startDate")]
        public DateTime StartDate { get; set; }

        [BsonElement("endDate")]
        public DateTime EndDate { get; set; }

        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;

        [BsonElement("isRecurring")]
        public bool IsRecurring { get; set; } = false;
    }

    public class SchoolDay
    {
        [BsonElement("date")]
        public DateTime Date { get; set; }

        [BsonElement("isSchoolDay")]
        public bool IsSchoolDay { get; set; } = true;

        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;
    }
}
