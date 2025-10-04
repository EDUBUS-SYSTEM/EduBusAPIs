using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Models.PickupPoint
{
	public class PickupPointDto
	{
		public Guid Id { get; set; }
		public string Description { get; set; } = string.Empty;
		public string Location { get; set; } = string.Empty;
		public double Latitude { get; set; }
		public double Longitude { get; set; }
		public int StudentCount { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
	}

	public class PickupPointsResponse
	{
		public List<PickupPointDto> PickupPoints { get; set; } = new List<PickupPointDto>();
		public int TotalCount { get; set; }
	}
}
