namespace Services.Contracts
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
        void QueueEmail(string to, string subject, string body);
    }
}
