namespace Services.Models.UserAccount
{
    public class ImportUserSuccess
    {
        public int RowNumber { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; 
        public Guid Id { get; set; }
    }
}
