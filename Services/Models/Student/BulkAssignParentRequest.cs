using System.ComponentModel.DataAnnotations;

namespace Services.Models.Student
{
    public class BulkAssignParentRequest
    {
        [Required(ErrorMessage = "Parent ID is required")]
        public Guid ParentId { get; set; }

        [Required(ErrorMessage = "Student IDs are required")]
        [MinLength(1, ErrorMessage = "At least one student must be specified")]
        public List<Guid> StudentIds { get; set; } = new();
    }
}
