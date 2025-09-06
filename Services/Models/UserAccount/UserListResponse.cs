using Data.Models.Enums;

namespace Services.Models.UserAccount
{
    public class UserListResponse
    {
        public List<UserDto> Users { get; set; } = new List<UserDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PerPage { get; set; }
        public int TotalPages { get; set; }
    }

    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Address { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public Gender Gender { get; set; }
        public Guid? UserPhotoFileId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
		public string Role { get; set; } = string.Empty;

		// Lock information
		public DateTime? LockedUntil { get; set; }
		public string? LockReason { get; set; }
		public DateTime? LockedAt { get; set; }
		public Guid? LockedBy { get; set; }
	}
}
