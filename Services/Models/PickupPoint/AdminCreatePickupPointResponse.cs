namespace Services.Models.PickupPoint
{
    public class AdminCreatePickupPointResponse
    {
        public Guid Id { get; set; }
        public Guid ParentId { get; set; }
        public string AddressText { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double DistanceKm { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<Guid> AssignedStudentIds { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }
}
