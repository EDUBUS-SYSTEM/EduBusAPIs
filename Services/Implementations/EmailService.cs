using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MimeKit.Text;
using MimeKit;
using Services.Contracts;
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
        MailKit.Net.Smtp.SmtpClient? smtp = null;
        try
        {
            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            msg.To.Add(MailboxAddress.Parse(to));
            msg.Subject = subject;
            msg.Body = new TextPart(TextFormat.Html) { Text = body };

            smtp = new MailKit.Net.Smtp.SmtpClient();
            var socketOpt = _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;

            await smtp.ConnectAsync(_settings.SmtpServer, _settings.SmtpPort, socketOpt, _cts.Token);
            await smtp.AuthenticateAsync(_settings.Username, _settings.Password, _cts.Token);
            await smtp.SendAsync(msg, _cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}. Error: {ErrorMessage}", to, ex.Message);
            throw new InvalidOperationException($"Failed to send email to {to}. Please check EmailSettings configuration.", ex);
        }
        finally
        {
            if (smtp != null && smtp.IsConnected)
            {
                try 
                { 
                    await smtp.DisconnectAsync(true, _cts.Token);
                } 
                catch { /* ignore disconnect errors */ }
            }
            smtp?.Dispose();
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
    private void ValidateSettings(EmailSettings s)
    {
        var missingFields = new List<string>();
        
        if (string.IsNullOrWhiteSpace(s.SmtpServer))
            missingFields.Add("SmtpServer");
        if (string.IsNullOrWhiteSpace(s.SenderEmail))
            missingFields.Add("SenderEmail");
        if (string.IsNullOrWhiteSpace(s.Username))
            missingFields.Add("Username");
        if (string.IsNullOrWhiteSpace(s.Password))
            missingFields.Add("Password");
            
        if (missingFields.Any())
        {
            var errorMsg = $"EmailSettings is incomplete. Missing fields: {string.Join(", ", missingFields)}. Please configure EmailSettings in appsettings.json or user secrets.";
            _logger.LogError("EmailService configuration error: {ErrorMessage}", errorMsg);
            throw new InvalidOperationException(errorMsg);
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

    private sealed record EmailTask
    {
        public required string To { get; init; }
        public required string Subject { get; init; }
        public required string Body { get; init; }
        public DateTime QueuedAt { get; init; }
    }
}

/// <summary>
/// Lightweight fire-and-forget email sender for new flows that do not need the background queue.
/// Register this separately (e.g. as ISimpleEmailService) without touching the legacy EmailService.
/// </summary>
public interface ISimpleEmailService
{
    void QueueEmail(string to, string subject, string body);
    Task SendEmailAsync(string to, string subject, string body);
}

public class SimpleEmailService : ISimpleEmailService
{
    private readonly SimpleEmailSettings _settings;
    private readonly ILogger<SimpleEmailService> _logger;
    private readonly CancellationTokenSource _cts = new();

    public SimpleEmailService(IConfiguration cfg, ILogger<SimpleEmailService> logger)
    {
        _logger = logger;
        _settings = new SimpleEmailSettings
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

    public void QueueEmail(string to, string subject, string body)
    {
        if (!MailboxAddress.TryParse(to, out _))
            throw new ArgumentException("Invalid recipient email address.", nameof(to));

        _ = Task.Run(async () =>
        {
            try
            {
                await SendEmailAsyncInternal(to, subject, body, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background email send failed to {To} with subject {Subject}", to, subject);
            }
        }, CancellationToken.None);
    }

    public Task SendEmailAsync(string to, string subject, string body)
    {
        return SendEmailAsyncInternal(to, subject, body, _cts.Token);
    }

    private async Task SendEmailAsyncInternal(string to, string subject, string body, CancellationToken token)
    {
        MailKit.Net.Smtp.SmtpClient? smtp = null;
        try
        {
            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            msg.To.Add(MailboxAddress.Parse(to));
            msg.Subject = subject;
            msg.Body = new TextPart(TextFormat.Html) { Text = body };

            smtp = new MailKit.Net.Smtp.SmtpClient();
            var socketOpt = _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
            await smtp.ConnectAsync(_settings.SmtpServer, _settings.SmtpPort, socketOpt, token);
            await smtp.AuthenticateAsync(_settings.Username, _settings.Password, token);
            await smtp.SendAsync(msg, token);
            _logger.LogInformation("Email sent to {To} - {Subject}", to, subject);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Email send to {To} cancelled", to);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} - {Subject}", to, subject);
            throw new InvalidOperationException($"Failed to send email to {to}", ex);
        }
        finally
        {
            if (smtp is { IsConnected: true })
            {
                try
                {
                    await smtp.DisconnectAsync(true, token);
                }
                catch (Exception disconnectEx)
                {
                    _logger.LogWarning(disconnectEx, "SMTP disconnect issue (non-critical)");
                }
            }

            smtp?.Dispose();
        }
    }

    private void ValidateSettings(SimpleEmailSettings settings)
    {
        var missingFields = new List<string>();
        if (string.IsNullOrWhiteSpace(settings.SmtpServer))
            missingFields.Add("SmtpServer");
        if (string.IsNullOrWhiteSpace(settings.SenderEmail))
            missingFields.Add("SenderEmail");
        if (string.IsNullOrWhiteSpace(settings.Username))
            missingFields.Add("Username");
        if (string.IsNullOrWhiteSpace(settings.Password))
            missingFields.Add("Password");

        if (missingFields.Any())
        {
            var errorMsg = $"Email settings missing: {string.Join(", ", missingFields)}";
            _logger.LogError("SimpleEmailService configuration error: {Message}", errorMsg);
            throw new InvalidOperationException(errorMsg);
        }
    }

    private sealed record SimpleEmailSettings
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
