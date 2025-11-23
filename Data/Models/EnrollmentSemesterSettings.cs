using MongoDB.Bson.Serialization.Attributes;

namespace Data.Models
{
    public class EnrollmentSemesterSettings : BaseMongoDocument
    {
        [BsonElement("semesterName")]
        public string SemesterName { get; set; } = string.Empty;

        [BsonElement("academicYear")]
        public string AcademicYear { get; set; } = string.Empty;

        [BsonElement("semesterCode")]
        public string SemesterCode { get; set; } = string.Empty;

        [BsonElement("semesterStartDate")]
        public DateTime SemesterStartDate { get; set; }

        [BsonElement("semesterEndDate")]
        public DateTime SemesterEndDate { get; set; }

        [BsonElement("registrationStartDate")]
        public DateTime RegistrationStartDate { get; set; }

        [BsonElement("registrationEndDate")]
        public DateTime RegistrationEndDate { get; set; }

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        [BsonElement("description")]
        public string? Description { get; set; }
    }
}

