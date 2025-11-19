using Services.Models.Common;

namespace Services.Models.SupervisorVehicle
{
    public class SupervisorAssignmentListResponse
    {
        public bool Success { get; set; }
        public List<SupervisorAssignmentDto> Data { get; set; } = new List<SupervisorAssignmentDto>();
        public PaginationInfo Pagination { get; set; } = new PaginationInfo();
        public string? Error { get; set; }
    }
}

