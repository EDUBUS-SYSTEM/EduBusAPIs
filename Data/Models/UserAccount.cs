using Data.Models.Enums;

namespace Data.Models;

public partial class UserAccount : BaseDomain
{
    public string Email { get; set; } = null!;

    public byte[] HashedPassword { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string PhoneNumber { get; set; }
    public string? Address { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    
    public Guid? UserPhotoFileId { get; set; }

	public DateTime? LockedUntil { get; set; }
	public string? LockReason { get; set; }
	public DateTime? LockedAt { get; set; }
	public Guid? LockedBy { get; set; }
}
