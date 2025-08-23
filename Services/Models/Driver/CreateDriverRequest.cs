using Services.Models.UserAccount;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Models.Driver
{
    public class CreateDriverRequest : CreateUserRequest
    {
        [Required(ErrorMessage = "License number is required.")]
        [StringLength(20, MinimumLength = 5, ErrorMessage = "License number must be between 5 and 20 characters.")]
        public string LicenseNumber { get; set; } = string.Empty;

    }
}
