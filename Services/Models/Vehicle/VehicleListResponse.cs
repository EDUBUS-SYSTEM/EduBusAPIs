using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Models.Vehicle
{
    public class VehicleListResponse
    {
        public bool Success { get; set; }
        public IEnumerable<VehicleDto> Data { get; set; } = new List<VehicleDto>();
        public object? Error { get; set; }
    }
}