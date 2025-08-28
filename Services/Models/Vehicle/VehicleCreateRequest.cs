using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Models.Vehicle
{
    public class VehicleCreateRequest
    {
        public string LicensePlate { get; set; } = null!;
        public int Capacity { get; set; }
        public string Status { get; set; } = null!;
        public Guid AdminId { get; set; }
    }
}
