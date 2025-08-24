namespace Data.Models;

public partial class DriverLicense : BaseDomain
{
    public byte[] HashedLicenseNumber { get; set; } = null!;
    
    public DateTime DateOfIssue { get; set; }
    
    public string IssuedBy { get; set; } = null!;
    
    public Guid? LicenseImageFileId { get; set; }
    
    public Guid CreatedBy { get; set; }
    
    public Guid? UpdatedBy { get; set; }
    
    public Guid DriverId { get; set; }
    
    // Navigation property
    public virtual Driver Driver { get; set; } = null!;
}
