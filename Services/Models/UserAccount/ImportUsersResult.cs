using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Models.UserAccount
{
    public class ImportUsersResult
    {
        public List<ImportUserSuccess> SuccessfulUsers { get; set; } = new List<ImportUserSuccess>();
        public List<ImportUserError> FailedUsers { get; set; } = new List<ImportUserError>();
        public int TotalProcessed { get; set; }
    }
}
