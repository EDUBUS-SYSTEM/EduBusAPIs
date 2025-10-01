using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Models.Route
{
    public class RouteDto
    {
        public Guid Id { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public Guid VehicleId { get; set; }
        public List<PickupPointInfoDto> PickupPoints { get; set; } = new List<PickupPointInfoDto>();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
		public int VehicleCapacity { get; set; }
        public string VehicleNumberPlate {  get; set; }
	}

    public class PickupPointInfoDto
    {
        public Guid PickupPointId { get; set; }
        public int SequenceOrder { get; set; }
        public LocationInfoDto Location { get; set; } = new LocationInfoDto();
		public int StudentCount { get; set; }
	}

    public class LocationInfoDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Address { get; set; } = string.Empty;
    }
}
