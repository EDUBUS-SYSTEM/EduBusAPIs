using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Models
{
    public class RefreshToken : BaseDomain
    {
        public Guid UserId { get; set; }
        public string Token { get; set; } = null!;
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? RevokedAtUtc { get; set; }
        public virtual UserAccount User { get; set; } = null!;
    }
}
