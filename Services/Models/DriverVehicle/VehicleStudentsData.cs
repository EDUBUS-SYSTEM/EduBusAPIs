namespace Services.Models.DriverVehicle
{
    /// <summary>
    /// Vehicle students data
    /// </summary>
    public class VehicleStudentsData
    {
        public Guid VehicleId { get; set; }
        public string? RouteId { get; set; }
        public string? RouteName { get; set; }
        public int TotalStudents { get; set; }
        public List<VehicleStudentInfo> Students { get; set; } = new List<VehicleStudentInfo>();
    }
}

