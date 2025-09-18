using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Models.Route
{
    public class RouteSuggestionDto
    {
        public Guid Id { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public List<PickupPointInfoDto> PickupPoints { get; set; } = new List<PickupPointInfoDto>();
        public VehicleInfo? Vehicle { get; set; }
        public int TotalStudents { get; set; }
        public double TotalDistance { get; set; } // in kilometers
        public TimeSpan TotalDuration { get; set; }
        public decimal EstimatedCost { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow; // Thêm property này
    }

    public class PickupPointInfo
    {
        public Guid PickupPointId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int SequenceOrder { get; set; }
        public int StudentCount { get; set; }
        public TimeSpan ArrivalTime { get; set; }
        public List<StudentInfo> Students { get; set; } = new List<StudentInfo>();
    }

    public class StudentInfo
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string ParentEmail { get; set; } = string.Empty;
    }

    public class VehicleInfo
    {
        public Guid VehicleId { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public int AssignedStudents { get; set; }
        public double UtilizationPercentage { get; set; }
    }
    public class VRPSettings
    {
        public int DefaultTimeLimitSeconds { get; set; } = 30;
        public bool UseTimeWindows { get; set; } = true;
        public string OptimizationType { get; set; } = "Distance";
        public int ServiceTimeSeconds { get; set; } = 300; // 5 minutes per pickup
    }
    public class PickupPointInfoDto
    {
        public Guid PickupPointId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int SequenceOrder { get; set; }
        public int StudentCount { get; set; }
        public TimeSpan ArrivalTime { get; set; }
        public List<StudentInfo> Students { get; set; } = new();
    }
}
