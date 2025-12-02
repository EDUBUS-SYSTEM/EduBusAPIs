using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Models.PickupPoint
{
    public class StudentBriefDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public StudentCurrentPickupPointDto? CurrentPickupPoint { get; set; }
        public bool HasCurrentPickupPoint => CurrentPickupPoint is not null;
    }

    public class StudentCurrentPickupPointDto
    {
        public Guid PickupPointId { get; set; }
        public string Description { get; set; } = "";
        public string Location { get; set; } = "";
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public DateTime? AssignedAt { get; set; }
    }
}
