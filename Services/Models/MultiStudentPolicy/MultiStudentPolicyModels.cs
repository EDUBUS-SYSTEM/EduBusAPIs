using System.ComponentModel.DataAnnotations;

namespace Services.Models.MultiStudentPolicy;
public class CreateMultiStudentPolicyDto
{
    [Required(ErrorMessage = "Policy name is required")]
    [MaxLength(200, ErrorMessage = "Policy name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Effective from date is required")]
    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    [Required(ErrorMessage = "Tiers list is required")]
    [MinLength(1, ErrorMessage = "At least 1 tier is required")]
    public List<CreatePolicyTierDto> Tiers { get; set; } = new();

    [Required(ErrorMessage = "Admin ID is required")]
    public Guid ByAdminId { get; set; }

    [Required(ErrorMessage = "Admin name is required")]
    public string ByAdminName { get; set; } = string.Empty;
}

public class CreatePolicyTierDto
{
    [Required(ErrorMessage = "Minimum student count is required")]
    [Range(2, int.MaxValue, ErrorMessage = "Minimum student count must be at least 2")]
    public int MinStudentCount { get; set; }

    [Range(2, int.MaxValue, ErrorMessage = "Maximum student count must be at least 2")]
    public int? MaxStudentCount { get; set; }

    [Required(ErrorMessage = "Reduction percentage is required")]
    [Range(0, 100, ErrorMessage = "Reduction percentage must be between 0 and 100")]
    public decimal ReductionPercentage { get; set; }

    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Priority is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Priority must be at least 1")]
    public int Priority { get; set; }
}

public class UpdateMultiStudentPolicyDto
{
    [Required(ErrorMessage = "Policy ID is required")]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Policy name is required")]
    [MaxLength(200, ErrorMessage = "Policy name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    [Required(ErrorMessage = "Effective from date is required")]
    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    [Required(ErrorMessage = "Tiers list is required")]
    [MinLength(1, ErrorMessage = "At least 1 tier is required")]
    public List<UpdatePolicyTierDto> Tiers { get; set; } = new();

    [Required(ErrorMessage = "Admin ID is required")]
    public Guid ByAdminId { get; set; }

    [Required(ErrorMessage = "Admin name is required")]
    public string ByAdminName { get; set; } = string.Empty;
}

public class UpdatePolicyTierDto
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Minimum student count is required")]
    [Range(2, int.MaxValue, ErrorMessage = "Minimum student count must be at least 2")]
    public int MinStudentCount { get; set; }

    [Range(2, int.MaxValue, ErrorMessage = "Maximum student count must be at least 2")]
    public int? MaxStudentCount { get; set; }

    [Required(ErrorMessage = "Reduction percentage is required")]
    [Range(0, 100, ErrorMessage = "Reduction percentage must be between 0 and 100")]
    public decimal ReductionPercentage { get; set; }

    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Priority is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Priority must be at least 1")]
    public int Priority { get; set; }
}

public class MultiStudentPolicyResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public List<PolicyTierResponseDto> Tiers { get; set; } = new();
    public Guid ByAdminId { get; set; }
    public string ByAdminName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class PolicyTierResponseDto
{
    public Guid Id { get; set; }
    public Guid PolicyId { get; set; }
    public int MinStudentCount { get; set; }
    public int? MaxStudentCount { get; set; }
    public decimal ReductionPercentage { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Priority { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PolicyCalculationResult
{
    public decimal ReductionAmount { get; set; }
    public decimal ReductionPercentage { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid? AppliedTierId { get; set; }
}

public class CalculatePerStudentRequest
{
    /// <summary>
    /// Number of students in the current registration (must be >=1)
    /// </summary>
    public int StudentCount { get; set; }

    /// <summary>
    /// Number of students of the same parent already registered/active for the same pickup point
    /// </summary>
    public int ExistingCount { get; set; } = 0;

    /// <summary>
    /// Optional original fee per student. If provided, reduction amounts will be calculated.
    /// </summary>
    public decimal? OriginalFeePerStudent { get; set; }
}

public class PerStudentDiscountResult
{
    public int Position { get; set; } // existingCount + index
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal OriginalFee { get; set; }
    public decimal FinalFee { get; set; }
    public Guid? AppliedTierId { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class CalculatePerStudentResponse
{
    public Guid? PolicyId { get; set; }
    public string PolicyName { get; set; } = string.Empty;
    public decimal BasePercentage { get; set; }
    public decimal MaxDiscountCap { get; set; } = 100m;
    public List<PerStudentDiscountResult> Students { get; set; } = new();
}

