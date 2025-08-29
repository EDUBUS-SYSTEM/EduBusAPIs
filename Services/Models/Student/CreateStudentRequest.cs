using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Models.Student
{
    public class CreateStudentRequest
    {
        public Guid? ParentId { get; set; }

        [Required(ErrorMessage = "First name is required.")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters.")]
        public string FirstName { get; set; } = null!;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters.")]
        public string LastName { get; set; } = null!;

        [StringLength(32, ErrorMessage = "Phone number cannot exceed 32 characters.")]
        [RegularExpression(@"^[0-9+\-\s()]+$", ErrorMessage = "Invalid phone number format.")]
        public string ParentPhoneNumber { get; set; } = string.Empty;

        // Custom validation
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!ParentId.HasValue && string.IsNullOrWhiteSpace(ParentPhoneNumber))
            {
                yield return new ValidationResult(
                    "Either ParentId or ParentPhoneNumber must be provided.",
                    new[] { nameof(ParentId), nameof(ParentPhoneNumber) });
            }
        }
    }
}
