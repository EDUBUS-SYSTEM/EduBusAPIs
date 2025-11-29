using MongoDB.Bson.Serialization.Attributes;

namespace Data.Models
{
    /// <summary>
    /// MongoDB document to log pickup point reset operations by semester
    /// </summary>
    public class PickupPointResetLog : BaseMongoDocument
    {
        [BsonElement("adminId")]
        public Guid AdminId { get; set; }

        [BsonElement("adminName")]
        public string? AdminName { get; set; }

        [BsonElement("semesterCode")]
        public string SemesterCode { get; set; } = string.Empty;

        [BsonElement("academicYear")]
        public string AcademicYear { get; set; } = string.Empty;

        [BsonElement("semesterName")]
        public string? SemesterName { get; set; }

        [BsonElement("semesterStartDate")]
        public DateTime SemesterStartDate { get; set; }

        [BsonElement("semesterEndDate")]
        public DateTime SemesterEndDate { get; set; }

        [BsonElement("totalRecordsFound")]
        public int TotalRecordsFound { get; set; }

        [BsonElement("studentsUpdated")]
        public int StudentsUpdated { get; set; }

        [BsonElement("studentsFailed")]
        public int StudentsFailed { get; set; }

        [BsonElement("updatedStudentIds")]
        public List<Guid> UpdatedStudentIds { get; set; } = new();

        [BsonElement("failedStudentIds")]
        public List<StudentUpdateFailureLog> FailedStudentIds { get; set; } = new();

        [BsonElement("resetAt")]
        public DateTime ResetAt { get; set; } = DateTime.UtcNow;

        [BsonElement("status")]
        public string Status { get; set; } = "Completed"; // "Completed", "Partial", "Failed"

        [BsonElement("message")]
        public string? Message { get; set; }
    }

    public class StudentUpdateFailureLog
    {
        [BsonElement("studentId")]
        public Guid StudentId { get; set; }

        [BsonElement("reason")]
        public string Reason { get; set; } = string.Empty;
    }
}

