using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Models.UserAccount
{
    public class AuthResponse
    {
        public string Token { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Role { get; set; } = null!;
        public DateTime ExpiresAtUtc { get; set; }

		// Lock information
		public bool IsLocked { get; set; }
		public DateTime? LockedUntil { get; set; }
		public string? LockReason { get; set; }
		public string? LockMessage { get; set; }
	}
}
