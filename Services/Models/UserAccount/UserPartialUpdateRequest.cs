using Data.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Services.Models.UserAccount
{
    public class UserPartialUpdateRequest
    {
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string? Email { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        [Phone(ErrorMessage = "Invalid phone number format.")]
        public string? PhoneNumber { get; set; }

        [Range(1, 3, ErrorMessage = "Gender must be between 1 and 3.")]
        public Gender? Gender { get; set; }

        [DataType(DataType.Date, ErrorMessage = "Invalid date format.")]
        public DateTime? DateOfBirth { get; set; }

        public string? Address { get; set; }
    }
}
