using System.ComponentModel.DataAnnotations;

namespace Services.Models.UnitPrice;

public class CreateUnitPriceDto : IValidatableObject
{
    [Required(ErrorMessage = "Service package name is required.")]
    [MaxLength(200, ErrorMessage = "Name cannot exceed 200 characters.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Price per km is required.")]
    [Range(1000, 1_000_000, ErrorMessage = "Price per km must be between 1,000 and 1,000,000 VND.")]
    public decimal PricePerKm { get; set; }

    [Required(ErrorMessage = "Effective from date is required.")]
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;

    public DateTime? EffectiveTo { get; set; }

    // Admin info will be automatically retrieved from JWT claims
    public Guid ByAdminId { get; set; }
    public string ByAdminName { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EffectiveTo.HasValue && EffectiveTo.Value <= EffectiveFrom)
        {
            yield return new ValidationResult("Effective end date must be after effective start date.", new[] { nameof(EffectiveTo) });
        }
    }
}
