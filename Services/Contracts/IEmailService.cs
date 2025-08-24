using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Contracts
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
        void QueueEmail(string to, string subject, string body);
    }
}
