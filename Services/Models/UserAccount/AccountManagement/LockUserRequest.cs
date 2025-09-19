namespace Services.Models.UserAccount.AccountManagement
{
	public class LockUserRequest
	{
		public DateTime? LockedUntil { get; set; }
		public string? Reason { get; set; }
	}
}
