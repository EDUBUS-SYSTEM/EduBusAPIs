using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Models.Vehicle
{
    public class VehiclePartialUpdateRequest
    {
        public string? LicensePlate { get; set; }
        public int? Capacity { get; set; }
        public string? Status { get; set; }
    }
}
