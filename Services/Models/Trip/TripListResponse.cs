using Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Models.Trip
{
	public class TripListResponse
	{
		public List<Data.Models.Trip> Trips { get; set; } = new List<Data.Models.Trip>(); // Return Trip entities, not DTOs
		public int TotalCount { get; set; }
		public int Page { get; set; }
		public int PerPage { get; set; }
		public int TotalPages { get; set; }
		public bool HasNextPage => Page < TotalPages;
		public bool HasPreviousPage => Page > 1;
	}
}
