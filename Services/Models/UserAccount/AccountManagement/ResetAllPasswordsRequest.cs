using System.ComponentModel.DataAnnotations;

namespace Services.Models.UserAccount.AccountManagement
{
    public class ResetAllPasswordsRequest
    {
        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
        public string Password { get; set; } = string.Empty;
    }
}

