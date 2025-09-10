using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MimeKit.Text;
using MimeKit;
using Services.Contracts;
using Services.Models.Email;
using System.Collections.Concurrent;

public class EmailService : IEmailService, IHostedService, IDisposable
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;
    private readonly ConcurrentQueue<EmailTask> _emailQueue = new();
    private Timer _timer = default!;
    private readonly CancellationTokenSource _cts = new();
    private const int MaxBatch = 5;
    private const int MaxQueueSize = 10_000;

    public EmailService(IConfiguration cfg, ILogger<EmailService> logger)
    {
        _logger = logger;

        // Load and validate settings once on startup
        _settings = new EmailSettings
        {
            SmtpServer = cfg["EmailSettings:SmtpServer"] ?? "",
            SmtpPort = int.TryParse(cfg["EmailSettings:SmtpPort"], out var p) ? p : 587,
            SenderEmail = cfg["EmailSettings:SenderEmail"] ?? "",
            SenderName = cfg["EmailSettings:SenderName"] ?? "EduBus",
            Username = cfg["EmailSettings:Username"] ?? "",
            Password = cfg["EmailSettings:Password"] ?? "",
            EnableSsl = bool.TryParse(cfg["EmailSettings:EnableSsl"], out var ssl) && ssl
        };
        ValidateSettings(_settings);
    }

    /// <summary>
    /// Start the background timer that periodically flushes the email queue.
    /// </summary>
    public Task StartAsync(CancellationToken ct)
    {
        _timer = new Timer(ProcessQueue, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stop the timer and best-effort flush the queue.
    /// </summary>
    public async Task StopAsync(CancellationToken ct)
    {
        _cts.Cancel();
        _timer?.Change(Timeout.Infinite, 0);

        // Best-effort flush for up to 5 seconds
        var stopAt = DateTime.UtcNow.AddSeconds(5);
        while (!_emailQueue.IsEmpty && DateTime.UtcNow < stopAt)
            await Task.Delay(200, ct);
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _cts.Dispose();
    }

    /// <summary>
    /// Enqueue an email to be sent asynchronously.
    /// Throws ArgumentException if email address is invalid.
    /// </summary>
    public void QueueEmail(string to, string subject, string body)
    {
        if (_emailQueue.Count >= MaxQueueSize)
        {
            _logger.LogWarning("Email queue is full; dropping message to {To}", to);
            return;
        }

        if (!MailboxAddress.TryParse(to, out _))
            throw new ArgumentException("Invalid recipient email address.", nameof(to));

        _emailQueue.Enqueue(new EmailTask
        {
            To = to,
            Subject = subject,
            Body = body,
            QueuedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Send an email immediately (synchronous to caller).
    /// </summary>
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
        msg.To.Add(MailboxAddress.Parse(to));
        msg.Subject = subject;
        msg.Body = new TextPart(TextFormat.Html) { Text = body };

        using var smtp = new MailKit.Net.Smtp.SmtpClient();

        // Choose TLS option based on config:
        // StartTls (587) or Auto (let MailKit decide).
        var socketOpt = _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;

        try
        {
            await smtp.ConnectAsync(_settings.SmtpServer, _settings.SmtpPort, socketOpt, _cts.Token);
            await smtp.AuthenticateAsync(_settings.Username, _settings.Password, _cts.Token);
            await smtp.SendAsync(msg, _cts.Token);
        }
        catch (Exception ex)
        {
            // Wrap with a concise, English error for higher layers/logs.
            _logger.LogError(ex, "Failed to send email to {To}.", to);
            throw new InvalidOperationException("Failed to send email. Please try again later.", ex);
        }
        finally
        {
            try { await smtp.DisconnectAsync(true, _cts.Token); } catch { /* ignore */ }
        }
    }

    /// <summary>
    /// Background worker that drains up to MaxBatch emails every tick.
    /// </summary>
    private async void ProcessQueue(object? _)
    {
        if (_cts.IsCancellationRequested) return;

        for (int i = 0; i < MaxBatch && _emailQueue.TryDequeue(out var task); i++)
        {
            try
            {
                await SendEmailAsync(task.To, task.Subject, task.Body);
            }
            catch (Exception ex)
            {
                // Do not rethrow on background loop; log and continue
                _logger.LogWarning(ex, "Background email send failed to {To}", task.To);
            }

            // Small pacing to avoid hammering SMTP server
            await Task.Delay(300, _cts.Token);
        }
    }

    /// <summary>
    /// Validate mandatory email settings at startup.
    /// </summary>
    private static void ValidateSettings(EmailSettings s)
    {
        if (string.IsNullOrWhiteSpace(s.SmtpServer) ||
            string.IsNullOrWhiteSpace(s.SenderEmail) ||
            string.IsNullOrWhiteSpace(s.Username) ||
            string.IsNullOrWhiteSpace(s.Password))
        {
            throw new InvalidOperationException("EmailSettings is incomplete. Please check configuration.");
        }
    }

    private sealed record EmailSettings
    {
        public string SmtpServer { get; init; } = "";
        public int SmtpPort { get; init; } = 587;
        public string SenderEmail { get; init; } = "";
        public string SenderName { get; init; } = "EduBus";
        public string Username { get; init; } = "";
        public string Password { get; init; } = "";
        public bool EnableSsl { get; init; } = true;
    }
}
