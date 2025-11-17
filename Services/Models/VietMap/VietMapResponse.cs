using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Services.Models.VietMap
{
    public class VietMapResponse
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("paths")]
        public List<Path>? Paths { get; set; }

        [JsonPropertyName("messages")]
        public List<string>? Messages { get; set; }
    }
    public class RouteResult
    {
        public double Distance { get; set; } // km
        public double Duration { get; set; } // seconds
        public double DurationMinutes { get; set; } // minutes
    }

    public class Path
    {
        [JsonPropertyName("distance")]
        public double Distance { get; set; } // meters

        [JsonPropertyName("time")]
        public double Time { get; set; } // milliseconds
    }
}
