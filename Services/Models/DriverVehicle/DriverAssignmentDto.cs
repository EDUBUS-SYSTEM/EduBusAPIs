using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data.Models.Enums;

namespace Services.Models.DriverVehicle
{
    public class DriverAssignmentDto
    {
        public Guid Id { get; set; }
        public Guid DriverId { get; set; }
        public Guid VehicleId { get; set; }
        public bool IsPrimaryDriver { get; set; }
        public DateTime StartTimeUtc { get; set; }
        public DateTime? EndTimeUtc { get; set; }
        public DriverVehicleStatus Status { get; set; }
        public string? AssignmentReason { get; set; }
        public Guid AssignedByAdminId { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public Guid? ApprovedByAdminId { get; set; }
        public string? ApprovalNote { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public DriverInfoDto? Driver { get; set; }
        public VehicleInfoDto? Vehicle { get; set; }
        public AdminInfoDto? AssignedByAdmin { get; set; }
        public AdminInfoDto? ApprovedByAdmin { get; set; }
        
        // Computed properties
        public bool IsActive { get; set; }
        public bool IsUpcoming { get; set; }
        public bool IsCompleted { get; set; }
        public TimeSpan? Duration { get; set; }
    }
}
