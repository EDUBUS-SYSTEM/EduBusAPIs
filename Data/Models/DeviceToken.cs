using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Models
{
    public class DeviceToken : BaseDomain
    {
        public Guid UserId { get; set; }
        public string Token { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty; // "ios", "android", "web"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastUsedAt { get; set; }
        public bool IsActive { get; set; } = true;
        public virtual UserAccount User { get; set; } = null!;
    }
}
