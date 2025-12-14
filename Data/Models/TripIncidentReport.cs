using System;
using Data.Models.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Data.Models
{
    public class TripIncidentReport : BaseMongoDocument
    {
        [BsonElement("tripId")]
        public Guid TripId { get; set; }

        [BsonElement("supervisorId")]
        public Guid SupervisorId { get; set; }

        [BsonElement("reason")]
        [BsonRepresentation(BsonType.String)]
        public TripIncidentReason Reason { get; set; }

        [BsonElement("title")]
        public string Title { get; set; } = string.Empty;

        [BsonElement("description")]
        public string? Description { get; set; }

        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public TripIncidentStatus Status { get; set; } = TripIncidentStatus.Open;

        [BsonElement("serviceDate")]
        public DateTime ServiceDate { get; set; }

        [BsonElement("tripStatus")]
        public string TripStatus { get; set; } = string.Empty;

        [BsonElement("routeName")]
        public string RouteName { get; set; } = string.Empty;

        [BsonElement("vehiclePlate")]
        public string VehiclePlate { get; set; } = string.Empty;

        [BsonElement("supervisorName")]
        public string SupervisorName { get; set; } = string.Empty;

        [BsonElement("adminNote")]
        public string? AdminNote { get; set; }

        [BsonElement("handledBy")]
        public Guid? HandledBy { get; set; }

        [BsonElement("handledAt")]
        public DateTime? HandledAt { get; set; }
    }
}

