using Data.Models.Enums;
using Services.Models.Common;

namespace Services.Models.Driver
{
    public class DriverLeaveListResponse
    {
        public bool Success { get; set; }
        public List<DriverLeaveResponse> Data { get; set; } = new List<DriverLeaveResponse>();
        public PaginationInfo Pagination { get; set; } = new PaginationInfo();
        public int PendingLeavesCount { get; set; }
        public object? Error { get; set; }
    }
}