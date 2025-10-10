using Services.Models.Common;

namespace Services.Models.DriverVehicle
{
    public class AssignmentListResponse
    {
        public bool Success { get; set; }
        public List<DriverAssignmentDto> Data { get; set; } = new List<DriverAssignmentDto>();
        public PaginationInfo Pagination { get; set; } = new PaginationInfo();
        public FilterSummary Filters { get; set; } = new FilterSummary();
        public object? Error { get; set; }
    }

    

    public class FilterSummary
    {
        public int TotalAssignments { get; set; }
        public int ActiveAssignments { get; set; }
        public int PendingAssignments { get; set; }
        public int CompletedAssignments { get; set; }
        public int CancelledAssignments { get; set; }
        public int SuspendedAssignments { get; set; }
        public int UpcomingAssignments { get; set; }
        public DateTime? EarliestStartDate { get; set; }
        public DateTime? LatestEndDate { get; set; }
    }
}
