namespace Services.Models.UserAccount
{
    public class CreateUserResponse
    {
        public Guid Id { get; set; }

        public string Email { get; set; } = string.Empty;
        public string Password {  get; set; } = string.Empty ;

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public DateTime DateOfBirth { get; set; }

        public string Address { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string Message { get; set; } = "User created successfully.";
    }
}
