using System.ComponentModel.DataAnnotations;

namespace Services.Models.PickupPoint
{
    public class ApprovePickupPointRequestDto
    {
        [Required] public Guid RequestId { get; set; }
        [MaxLength(500)] public string? Notes { get; set; }
    }
}
