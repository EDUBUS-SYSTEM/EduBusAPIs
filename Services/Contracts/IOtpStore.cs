namespace Services.Contracts
{
    public interface IOtpStore
    {
        Task<bool> SaveAsync(string purpose, string email, string otpHash, TimeSpan ttl, bool overwrite = false);
        Task<(string? Hash, int Attempts, int MaxAttempts)> GetAsync(string purpose, string email);
        Task<(bool Allowed, int Attempts, int MaxAttempts)> IncrementAttemptsAsync(string purpose, string email);
        Task DeleteAsync(string purpose, string email);
    }

}
