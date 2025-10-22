namespace Services.Models.DriverVehicle
{
    /// <summary>
    /// Vehicle student information for driver's vehicle
    /// </summary>
    public class VehicleStudentInfo
    {
        public Guid StudentId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public Guid PickupPointId { get; set; }
        public string PickupPointAddress { get; set; } = string.Empty;
        public int PickupSequenceOrder { get; set; }
        public string? GradeLevel { get; set; }
        public string? ParentName { get; set; }
        public string? ParentPhone { get; set; }
    }
}

