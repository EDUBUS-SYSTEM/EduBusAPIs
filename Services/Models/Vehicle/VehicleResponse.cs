using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Models.Vehicle
{
    public class VehicleResponse
    {
        public bool Success { get; set; }
        public VehicleDto? Data { get; set; }
        public object? Error { get; set; }
    }
}
