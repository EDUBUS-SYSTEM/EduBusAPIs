using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Models.Route
{
    public class RouteSuggestionRequest
    {
        public double? SchoolLatitude { get; set; }
        public double? SchoolLongitude { get; set; }
        public DateTime ServiceDate { get; set; } = DateTime.Today;
    }
}
