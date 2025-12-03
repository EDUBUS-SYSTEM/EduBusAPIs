using Microsoft.Extensions.Caching.Memory;

namespace Services.Helpers
{
    public interface IOtpService
    {
        string GenerateOtp();
        void StoreOtp(string email, string otp);
        bool VerifyOtp(string email, string otp);
        void ClearOtp(string email);
    }

    public class OtpService : IOtpService
    {
        private readonly IMemoryCache _cache;
        private const int OtpLength = 6;
        private const int OtpExpiryMinutes = 10;

        public OtpService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public string GenerateOtp()
        {
            var random = new Random();
            var otp = random.Next(0, 1000000).ToString("D6");
            return otp;
        }

        public void StoreOtp(string email, string otp)
        {
            var cacheKey = GetCacheKey(email);
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(OtpExpiryMinutes)
            };
            _cache.Set(cacheKey, otp, cacheOptions);
        }

        public bool VerifyOtp(string email, string otp)
        {
            var cacheKey = GetCacheKey(email);
            if (_cache.TryGetValue(cacheKey, out string? storedOtp))
            {
                return storedOtp == otp;
            }
            return false;
        }

        public void ClearOtp(string email)
        {
            var cacheKey = GetCacheKey(email);
            _cache.Remove(cacheKey);
        }

        private string GetCacheKey(string email)
        {
            return $"OTP_{email.ToLower()}";
        }
    }
}
