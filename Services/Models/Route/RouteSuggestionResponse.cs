using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Models.Route
{
    public class RouteSuggestionResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<RouteSuggestionDto> Routes { get; set; } = new List<RouteSuggestionDto>();
        public int TotalRoutes { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
