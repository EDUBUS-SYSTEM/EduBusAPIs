using System.ComponentModel.DataAnnotations;

namespace Services.Models.PickupPoint
{
    public class RejectPickupPointRequestDto
    {
        [Required] public Guid RequestId { get; set; }
        [Required, MaxLength(500)] public string Reason { get; set; } = "";
    }
}
