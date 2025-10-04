using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Models.Vehicle
{
    public class VehicleListResponse
    {
        public List<VehicleDto> Vehicles { get; set; } = new List<VehicleDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PerPage { get; set; }
        public int TotalPages { get; set; }
    }
}