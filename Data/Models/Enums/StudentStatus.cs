namespace Data.Models.Enums
{
    public enum StudentStatus
    {
        Available = 0,   // Student created but no service request yet
        Pending = 1,     // Service request submitted, waiting for approval
        Active = 2,      // Approved and actively using service or paid
        Inactive = 3,    // Temporarily stopped using service
        Deleted = 4      // Soft-deleted by admin
    }
}