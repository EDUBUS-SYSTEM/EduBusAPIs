using Data.Models.Enums;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Models
{
    public class StudentAbsenceRequest : BaseMongoDocument
    {
        [BsonElement("studentId")]
        public Guid StudentId { get; set; }

        [BsonElement("parentId")]
        public Guid ParentId { get; set; }

        [BsonElement("startDate")]
        public DateTime StartDate { get; set; }

        [BsonElement("endDate")]
        public DateTime EndDate { get; set; }

        [BsonElement("studentName")]
        public string StudentName { get; set; } = string.Empty;

        [BsonElement("parentName")]
        public string ParentName { get; set; } = string.Empty;

        [BsonElement("parentEmail")]
        public string ParentEmail { get; set; } = string.Empty;

        [BsonElement("parentPhoneNumber")]
        public string ParentPhoneNumber { get; set; } = string.Empty;

        [BsonElement("reason")]
        public string Reason { get; set; } = string.Empty;

        [BsonElement("notes")]
        public string? Notes { get; set; }

        [BsonElement("status")]
        public AbsenceRequestStatus Status { get; set; } = AbsenceRequestStatus.Pending;

        [BsonElement("reviewedBy")]
        public Guid? ReviewedBy { get; set; }

        [BsonElement("reviewedAt")]
        public DateTime? ReviewedAt { get; set; }
    }
}
