using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Data.Models
{
    public class Route : BaseMongoDocument
    {
        [BsonElement("routeName")]
        public string RouteName { get; set; } = string.Empty;

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        [BsonElement("vehicle_id")]
        public Guid VehicleId { get; set; }

        [BsonElement("pickup_points")]
        public List<PickupPointInfo> PickupPoints { get; set; } = new List<PickupPointInfo>();
    }

    public class PickupPointInfo
    {
        [BsonElement("pickupPointId")]
        public Guid PickupPointId { get; set; }

        [BsonElement("sequenceOrder")]
        public int SequenceOrder { get; set; }

        [BsonElement("location")]
        public LocationInfo Location { get; set; } = new LocationInfo();
    }

    public class LocationInfo
    {
        [BsonElement("latitude")]
        public double Latitude { get; set; }

        [BsonElement("longitude")]
        public double Longitude { get; set; }

        [BsonElement("address")]
        public string Address { get; set; } = string.Empty;
    }
}
