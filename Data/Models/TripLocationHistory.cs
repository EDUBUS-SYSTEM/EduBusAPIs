using MongoDB.Bson.Serialization.Attributes;

namespace Data.Models
{
    public class TripLocationHistory : BaseMongoDocument
    {
        [BsonElement("tripId")]
        public Guid TripId { get; set; }

        [BsonElement("location")]
        public Trip.VehicleLocation Location { get; set; } = new Trip.VehicleLocation();
    }
}


