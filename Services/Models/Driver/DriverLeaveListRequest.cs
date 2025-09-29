using Data.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Services.Models.Driver
{
    public class DriverLeaveListRequest
    {
        [StringLength(100, ErrorMessage = "Search term cannot exceed 100 characters.")]
        public string? SearchTerm { get; set; }
        public LeaveStatus? Status { get; set; }
        public LeaveType? LeaveType { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0.")]
        public int Page { get; set; } = 1;

        [Range(1, 100, ErrorMessage = "PerPage must be between 1 and 100.")]
        public int PerPage { get; set; } = 10;

        [StringLength(10, ErrorMessage = "SortOrder cannot exceed 10 characters.")]
        public string SortOrder { get; set; } = "desc";
    }
}


