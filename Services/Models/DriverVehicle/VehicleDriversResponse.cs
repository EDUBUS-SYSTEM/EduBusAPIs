using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Models.DriverVehicle
{
    public class VehicleDriversResponse
    {
        public bool Success { get; set; }
        public IEnumerable<DriverAssignmentDto> Data { get; set; } = new List<DriverAssignmentDto>();
        public object? Error { get; set; }
    }
}
