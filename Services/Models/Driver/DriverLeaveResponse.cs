using Data.Models.Enums;

namespace Services.Models.Driver
{
    public class DriverLeaveResponse
    {
        public Guid Id { get; set; }
        public Guid DriverId { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public string DriverEmail { get; set; } = string.Empty;
        public string DriverPhoneNumber { get; set; } = string.Empty;
        public string DriverLicenseNumber { get; set; } = string.Empty;
        public LeaveType LeaveType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public LeaveStatus Status { get; set; }
        public DateTime RequestedAt { get; set; }

        // Primary vehicle information
        public Guid? PrimaryVehicleId { get; set; }
        public string? PrimaryVehicleLicensePlate { get; set; }

        // Approval information 
        public Guid? ApprovedByAdminId { get; set; }
        public string? ApprovedByAdminName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovalNote { get; set; }
        
        // Auto-replacement information
        public bool AutoReplacementEnabled { get; set; }
        public Guid? SuggestedReplacementDriverId { get; set; }
        public string? SuggestedReplacementDriverName { get; set; }
        public Guid? SuggestedReplacementVehicleId { get; set; }
        public string? SuggestedReplacementVehiclePlate { get; set; }
        public DateTime? SuggestionGeneratedAt { get; set; }
        
        // Additional information
        public string? AdditionalInformation { get; set; }
    }
}
