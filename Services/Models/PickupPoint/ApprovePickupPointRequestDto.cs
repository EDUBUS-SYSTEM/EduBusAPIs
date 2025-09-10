using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Models.PickupPoint
{
    public class ApprovePickupPointRequestDto
    {
        [Required] public Guid RequestId { get; set; }
        [MaxLength(500)] public string? Notes { get; set; }
    }
}
