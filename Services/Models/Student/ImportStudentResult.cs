using Services.Models.UserAccount;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Models.Student
{
    public class ImportStudentResult
    {
        public List<ImportStudentSuccess> SuccessfulStudents { get; set; } = new List<ImportStudentSuccess>();
        public List<ImportStudentError> FailedStudents { get; set; } = new List<ImportStudentError>();
        public int TotalProcessed { get; set; }
    }
}
