using System.ComponentModel.DataAnnotations;

namespace Services.Models.Student
{
    public class CreateStudentRequest : IValidatableObject
    {
        public Guid? ParentId { get; set; }

        [Required(ErrorMessage = "First name is required.")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters.")]
        public string FirstName { get; set; } = null!;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters.")]
        public string LastName { get; set; } = null!;

        [StringLength(256)]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string ParentEmail { get; set; } = string.Empty;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!ParentId.HasValue && string.IsNullOrWhiteSpace(ParentEmail))
            {
                yield return new ValidationResult(
                    "Either ParentId or ParentEmail must be provided.",
                    new[] { nameof(ParentId), nameof(ParentEmail) });
            }
        }
    }
}
