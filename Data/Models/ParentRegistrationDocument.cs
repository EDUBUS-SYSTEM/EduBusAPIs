namespace Data.Models
{
    public class ParentRegistrationDocument : BaseMongoDocument
    {
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public int Gender { get; set; }
        
        public string Status { get; set; } = "Pending"; // Pending, Verified, Expired
        public DateTime? VerifiedAt { get; set; }
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24); // Expire after 24 hours
    }
}
