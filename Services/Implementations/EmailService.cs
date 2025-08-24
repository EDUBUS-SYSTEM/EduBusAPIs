using DocumentFormat.OpenXml.Vml;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MimeKit;
using MimeKit.Text;
using Services.Contracts;
using Services.Models.Email;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Services.Implementations
{
    public class EmailService : IEmailService, IHostedService
    {
        private readonly IConfiguration _config;
        private readonly ConcurrentQueue<EmailTask> _emailQueue;
        private readonly Timer _timer;
        private readonly CancellationTokenSource _cancellationToken;

        public EmailService(IConfiguration config)
        {
            _config = config;
            _emailQueue = new ConcurrentQueue<EmailTask>();
            _cancellationToken = new CancellationTokenSource();

            // Process queue every 2 seconds
            _timer = new Timer(ProcessQueue, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
        }
        public async Task SendEmailAsync(string to, string subject, string body)
        {

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(
                _config["EmailSettings:SenderName"],
                _config["EmailSettings:SenderEmail"]));

            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Html) { Text = body };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _config["EmailSettings:SmtpServer"],
                int.Parse(_config["EmailSettings:SmtpPort"]),
                SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync(
                _config["EmailSettings:Username"],
                _config["EmailSettings:Password"]);

            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
        public void QueueEmail(string to, string subject, string body)
        {
            var emailTask = new EmailTask
            {
                To = to,
                Subject = subject,
                Body = body,
                QueuedAt = DateTime.Now
            };

            _emailQueue.Enqueue(emailTask);
        }
        private async void ProcessQueue(object state)
        {
            if (_cancellationToken.Token.IsCancellationRequested)
                return;

            // Process up to 5 emails at a time
            for (int i = 0; i < 5 && _emailQueue.TryDequeue(out var emailTask); i++)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await SendEmailAsync(emailTask.To, emailTask.Subject, emailTask.Body);
                    }
                    catch (Exception ex)
                    {
                    }
                });

                // Small delay between emails to avoid overwhelming SMTP server
                await Task.Delay(500);
            }
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellationToken.Cancel();
            _timer?.Dispose();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _cancellationToken?.Dispose();
        }

    }
}
