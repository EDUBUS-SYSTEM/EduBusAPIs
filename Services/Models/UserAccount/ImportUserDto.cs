namespace Services.Models.UserAccount
{
    public class ImportUserDto
    {
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public int Gender { get; set; }
        public string DateOfBirthString { get; set; }
        public string Address { get; set; } = string.Empty;
    }
}
