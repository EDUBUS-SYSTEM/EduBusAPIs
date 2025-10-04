using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Models.Common
{
    public class PaginationInfo
    {
        public int CurrentPage { get; set; }
        public int PerPage { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }
}
