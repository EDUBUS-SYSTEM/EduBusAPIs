namespace Services.Models.UserAccount.AccountManagement
{
	public class UnlockMultipleUsersRequest
	{
		public List<Guid> UserIds { get; set; } = new();
	}
}
