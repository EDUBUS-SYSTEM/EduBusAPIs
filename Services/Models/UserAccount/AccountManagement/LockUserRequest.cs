using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Models.UserAccount.AccountManagement
{
	public class LockUserRequest
	{
		public DateTime? LockedUntil { get; set; }
		public string? Reason { get; set; }
	}
}
