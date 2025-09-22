namespace Data.Models.Enums
{
    public enum StudentStatus
    {
        Available = 0,   // Student created but no service request yet, Service request submitted
        Pending = 1,     // Approved by admin
        Active = 2,      //  Payemnt transaction is paid
        Inactive = 3,    // Temporarily stopped using service
        Deleted = 4      // Soft-deleted by admin
    }
}