using System.ComponentModel.DataAnnotations;

namespace Services.Models.PickupPoint
{
    public class PickupPointRequestListQuery
    {
        public string? Status { get; set; }      // "Pending" / "Approved" / "Rejected"
        public string? ParentEmail { get; set; } 
        [Range(0, int.MaxValue)] public int Skip { get; set; } = 0;
        [Range(1, 200)] public int Take { get; set; } = 50;
    }
}
