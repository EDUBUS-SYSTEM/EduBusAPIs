using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Models.Student
{
    public class ImportStudentError
    {
        public int RowNumber { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string ParentPhoneNumber { get; set; } = null!;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
