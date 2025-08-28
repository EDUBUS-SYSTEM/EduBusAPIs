using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Models.DriverVehicle
{
    public class DriverAssignmentResponse
    {
        public bool Success { get; set; }
        public DriverAssignmentDto? Data { get; set; }
        public object? Error { get; set; }
    }
}
