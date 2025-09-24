using System.ComponentModel.DataAnnotations;
using Data.Models.Enums;

namespace Services.Models.DriverVehicle
{
    public class AssignmentListRequest
    {
        // Filter parameters
        public Guid? DriverId { get; set; }
        public Guid? VehicleId { get; set; }
        public DriverVehicleStatus? Status { get; set; }
        public bool? IsPrimaryDriver { get; set; }
        public DateTime? StartDateFrom { get; set; }
        public DateTime? StartDateTo { get; set; }
        public DateTime? EndDateFrom { get; set; }
        public DateTime? EndDateTo { get; set; }
        public Guid? AssignedByAdminId { get; set; }
        public Guid? ApprovedByAdminId { get; set; }
        public bool? IsActive { get; set; } // Current time within start-end range
        public bool? IsUpcoming { get; set; } // Start time in future
        public bool? IsCompleted { get; set; } // End time in past
        
        // Search parameters
        public string? SearchTerm { get; set; } // Search in driver name, vehicle plate, assignment reason
        
        // Pagination
        [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
        public int Page { get; set; } = 1;
        
        [Range(1, 100, ErrorMessage = "PerPage must be between 1 and 100")]
        public int PerPage { get; set; } = 20;
        
        // Sorting
        public string? SortBy { get; set; } = "createdAt"; // createdAt, startTime, endTime, driverName, vehiclePlate, status
        public string SortOrder { get; set; } = "desc"; // asc, desc
    }
}
