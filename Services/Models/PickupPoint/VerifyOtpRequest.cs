using System.ComponentModel.DataAnnotations;

namespace Services.Models.PickupPoint
{
    public class VerifyOtpRequest
    {
        [Required, EmailAddress, MaxLength(320)]
        public string Email { get; set; } = "";
        [Required, RegularExpression(@"^\d{4,8}$")]
        public string Otp { get; set; } = "";
    }
}
