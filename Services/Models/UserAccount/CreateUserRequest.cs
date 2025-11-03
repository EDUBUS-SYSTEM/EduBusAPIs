using Data.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Services.Models.UserAccount
{
    public class CreateUserRequest
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Invalid phone number format.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Gender is required.")]
        [Range(1, 3, ErrorMessage = "Gender must be greater than 0.")]
        public Gender Gender { get; set; }

        [Required(ErrorMessage = "Date of birth is required.")]
        [DataType(DataType.Date, ErrorMessage = "Invalid date format.")]
        [CustomValidation(typeof(CreateUserRequest), "ValidateDateOfBirth")]
        public DateTime DateOfBirth { get; set; }

        public static ValidationResult? ValidateDateOfBirth(DateTime dateOfBirth, ValidationContext context)
        {
            var today = DateTime.Today;
            
            // Check if date is in the future
            if (dateOfBirth > today)
            {
                return new ValidationResult("Date of birth cannot be in the future.");
            }
            
            // Check if person is at least 18 years old
            var age = today.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > today.AddYears(-age))
                age--;

            if (age < 18)
            {
                return new ValidationResult("Person must be at least 18 years old.");
            }
            
            // Check if date is not too far in the past (reasonable age limit, e.g., 100 years)
            if (age > 100)
            {
                return new ValidationResult("Date of birth seems invalid. Please check the year.");
            }

            return ValidationResult.Success;
        }

        [Required(ErrorMessage = "Address is required.")]
        public string Address { get; set; } = string.Empty;

    }
}
