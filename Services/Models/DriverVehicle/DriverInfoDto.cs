namespace Services.Models.DriverVehicle
{
    public class DriverInfoDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string Status { get; set; } = null!;
    }
}
