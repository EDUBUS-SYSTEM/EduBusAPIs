using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Models.Email
{
    public class EmailTask
    {
        public string To { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public DateTime QueuedAt { get; set; }
    }
}
