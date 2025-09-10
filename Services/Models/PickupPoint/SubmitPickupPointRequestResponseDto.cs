namespace Services.Models.PickupPoint
{
    public class SubmitPickupPointRequestResponseDto
    {
        public Guid RequestId { get; set; }
        public string Status { get; set; } = "Pending";
        public string Message { get; set; } = string.Empty;
        public decimal EstimatedPriceVnd { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
