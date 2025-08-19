namespace Data.Models;

public partial class UserAccount : BaseDomain
{
    public string Email { get; set; } = null!;

    public byte[] HashedPassword { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string? PhoneNumber { get; set; }
}
