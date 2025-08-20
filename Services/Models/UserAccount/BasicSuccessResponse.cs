using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Models.UserAccount
{
    public class BasicSuccessResponse
    {
        public bool Success { get; set; }
        public object? Data { get; set; }
        public object? Error { get; set; }
    }
}
