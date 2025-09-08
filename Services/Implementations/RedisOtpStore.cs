using Microsoft.EntityFrameworkCore.Storage;
using Services.Contracts;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using IDatabase = StackExchange.Redis.IDatabase;

namespace Services.Implementations
{
    public class RedisOtpStore : IOtpStore
    {
        private readonly IDatabase _db;
        private const int DefaultMaxAttempts = 5;
        public RedisOtpStore(IConnectionMultiplexer mux) => _db = mux.GetDatabase();

        private static string Key(string purpose, string email)
            => $"otp:{purpose}:{email}".ToLowerInvariant();

        private record Payload(string Hash, int Attempts, int MaxAttempts, DateTime IssuedAtUtc);

        public async Task<bool> SaveAsync(string purpose, string email, string otpHash, TimeSpan ttl, bool overwrite = false)
        {
            var key = Key(purpose, email);
            var json = JsonSerializer.Serialize(new Payload(otpHash, 0, DefaultMaxAttempts, DateTime.UtcNow));
            return !overwrite
                ? await _db.StringSetAsync(key, json, ttl, when: When.NotExists)
                : await _db.StringSetAsync(key, json, ttl);
        }

        public async Task<(string? Hash, int Attempts, int MaxAttempts)> GetAsync(string purpose, string email)
        {
            var raw = await _db.StringGetAsync(Key(purpose, email));
            if (raw.IsNullOrEmpty) return (null, 0, 0);
            var p = JsonSerializer.Deserialize<Payload>(raw!)!;
            return (p.Hash, p.Attempts, p.MaxAttempts);
        }

        public async Task<(bool Allowed, int Attempts, int MaxAttempts)> IncrementAttemptsAsync(string purpose, string email)
        {
            var key = Key(purpose, email);
            var ttl = await _db.KeyTimeToLiveAsync(key) ?? TimeSpan.FromMinutes(5);

            var raw = await _db.StringGetAsync(key);
            if (raw.IsNullOrEmpty) return (false, 0, 0);

            var p = JsonSerializer.Deserialize<Payload>(raw!)!;
            var attempts = p.Attempts + 1;
            var updated = p with { Attempts = attempts };
            await _db.StringSetAsync(key, JsonSerializer.Serialize(updated), ttl);
            return (attempts <= p.MaxAttempts, attempts, p.MaxAttempts);
        }

        public Task DeleteAsync(string purpose, string email)
            => _db.KeyDeleteAsync(Key(purpose, email));
    }
}
