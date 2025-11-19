using System.Collections.Generic;
using Services.Models.Common;

namespace Services.Models.StudentAbsenceRequest
{
    public class StudentAbsenceRequestListResponse
    {
        public List<StudentAbsenceRequestListItemDto> Data { get; set; } = new();
        public PaginationInfo Pagination { get; set; } = new PaginationInfo();
    }
}

