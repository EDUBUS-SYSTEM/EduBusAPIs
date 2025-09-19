namespace Services.Models.Vehicle
{
    public class VehicleDto
    {
        public Guid Id { get; set; }
        public int Capacity { get; set; }
        public string LicensePlate { get; set; } = null!;
        public string Status { get; set; } = null!;
        public Guid AdminId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }
}
