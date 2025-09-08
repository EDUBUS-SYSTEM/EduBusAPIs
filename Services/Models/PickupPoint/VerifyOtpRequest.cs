using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
