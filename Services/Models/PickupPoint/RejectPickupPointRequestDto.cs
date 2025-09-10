using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Models.PickupPoint
{
    public class RejectPickupPointRequestDto
    {
        [Required] public Guid RequestId { get; set; }
        [Required, MaxLength(500)] public string Reason { get; set; } = "";
    }
}
