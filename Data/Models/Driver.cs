namespace Data.Models;

public partial class Driver : UserAccount
{
    public Guid? HealthCertificateFileId { get; set; }

    public virtual ICollection<DriverVehicle> DriverVehicles { get; set; } = new List<DriverVehicle>();
    
    public virtual DriverLicense? DriverLicense { get; set; }
}
