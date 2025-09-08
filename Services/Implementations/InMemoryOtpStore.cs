using Services.Contracts;
using System.Collections.Concurrent;

namespace Services.Implementations
{
	public class InMemoryOtpStore : IOtpStore
	{
		private class Entry
		{
			public string Hash { get; set; } = string.Empty;
			public int Attempts; // field for Interlocked operations
			public int MaxAttempts { get; set; } = 5;
			public DateTime ExpiresAtUtc { get; set; }
		}

		private static readonly ConcurrentDictionary<string, Entry> Store = new();

		private static string Key(string purpose, string email)
			=> $"otp:{purpose}:{email}".ToLowerInvariant();

		public Task<bool> SaveAsync(string purpose, string email, string otpHash, TimeSpan ttl, bool overwrite = false)
		{
			var key = Key(purpose, email);
			var entry = new Entry
			{
				Hash = otpHash,
				Attempts = 0,
				MaxAttempts = 5,
				ExpiresAtUtc = DateTime.UtcNow.Add(ttl)
			};

			if (!overwrite)
			{
				var added = Store.TryAdd(key, entry);
				return Task.FromResult(added);
			}

			Store[key] = entry;
			return Task.FromResult(true);
		}

		public Task<(string? Hash, int Attempts, int MaxAttempts)> GetAsync(string purpose, string email)
		{
			var key = Key(purpose, email);
			if (Store.TryGetValue(key, out var entry))
			{
				if (DateTime.UtcNow > entry.ExpiresAtUtc)
				{
					Store.TryRemove(key, out _);
					return Task.FromResult<(string?, int, int)>((null, 0, 0));
				}
				return Task.FromResult<(string?, int, int)>((entry.Hash, entry.Attempts, entry.MaxAttempts));
			}
			return Task.FromResult<(string?, int, int)>((null, 0, 0));
		}

		public Task<(bool Allowed, int Attempts, int MaxAttempts)> IncrementAttemptsAsync(string purpose, string email)
		{
			var key = Key(purpose, email);
			if (!Store.TryGetValue(key, out var entry))
				return Task.FromResult((false, 0, 0));

			if (DateTime.UtcNow > entry.ExpiresAtUtc)
			{
				Store.TryRemove(key, out _);
				return Task.FromResult((false, 0, 0));
			}

			var attempts = Interlocked.Increment(ref entry.Attempts);
			return Task.FromResult((attempts <= entry.MaxAttempts, attempts, entry.MaxAttempts));
		}

		public Task DeleteAsync(string purpose, string email)
		{
			var key = Key(purpose, email);
			Store.TryRemove(key, out _);
			return Task.CompletedTask;
		}
	}
}
