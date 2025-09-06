using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Models.UserAccount.AccountManagement
{
	public class UnlockMultipleUsersRequest
	{
		public List<Guid> UserIds { get; set; } = new();
	}
}
