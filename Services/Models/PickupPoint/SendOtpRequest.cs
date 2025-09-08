using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Models.PickupPoint
{
    public class SendOtpRequest
    {
        [Required, EmailAddress, MaxLength(320)]
        public string Email { get; set; } = "";
    }
}
