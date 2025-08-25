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
            try
            {
                // Validate configuration
                var smtpServer = _config["EmailSettings:SmtpServer"];
                var smtpPort = _config["EmailSettings:SmtpPort"];
                var senderEmail = _config["EmailSettings:SenderEmail"];
                var senderName = _config["EmailSettings:SenderName"];
                var username = _config["EmailSettings:Username"];
                var password = _config["EmailSettings:Password"];

                if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(smtpPort) || 
                    string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(username) || 
                    string.IsNullOrEmpty(password))
                {
                    throw new InvalidOperationException("Email configuration is incomplete. Please check EmailSettings in configuration.");
                }

                Console.WriteLine($"Attempting to send email to: {to}");
                Console.WriteLine($"SMTP Server: {smtpServer}:{smtpPort}");

                var email = new MimeMessage();
                email.From.Add(new MailboxAddress(senderName, senderEmail));
                email.To.Add(MailboxAddress.Parse(to));
                email.Subject = subject;
                email.Body = new TextPart(TextFormat.Html) { Text = body };

                using var smtp = new SmtpClient();
                
                Console.WriteLine("Connecting to SMTP server...");
                await smtp.ConnectAsync(smtpServer, int.Parse(smtpPort), SecureSocketOptions.StartTls);
                
                Console.WriteLine("Authenticating...");
                await smtp.AuthenticateAsync(username, password);
                
                Console.WriteLine("Sending email...");
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
                
                Console.WriteLine($"Email sent successfully to: {to}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email to {to}: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                throw;
            }
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
