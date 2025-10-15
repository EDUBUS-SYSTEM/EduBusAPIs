using Data.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Services.Models.Student
{
    public class UpdateStudentRequest
    {
        public Guid? ParentId { get; set; }

        [Required(ErrorMessage = "First name is required.")]
        [StringLength(50)]
        public string FirstName { get; set; } = null!;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(50)]
        public string LastName { get; set; } = null!;

        [Required(ErrorMessage = "Parent email is required.")]
        [StringLength(256)]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string ParentEmail { get; set; } = string.Empty;
    }
}
