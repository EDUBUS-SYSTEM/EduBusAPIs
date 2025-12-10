using Services.Models.MultiStudentPolicy;

namespace Services.Contracts;

public interface IMultiStudentPolicyService
{
    /// <summary>
    /// Get all policies
    /// </summary>
    Task<List<MultiStudentPolicyResponseDto>> GetAllPoliciesAsync();

    /// <summary>
    /// Get all policies (including deleted)
    /// </summary>
    Task<List<MultiStudentPolicyResponseDto>> GetAllIncludingDeletedAsync();

    /// <summary>
    /// Get policy by ID
    /// </summary>
    Task<MultiStudentPolicyResponseDto?> GetPolicyByIdAsync(Guid id);

    /// <summary>
    /// Get current active and effective policy
    /// </summary>
    Task<MultiStudentPolicyResponseDto?> GetCurrentActivePolicyAsync();

    /// <summary>
    /// Calculate policy reduction based on student count
    /// </summary>
    Task<PolicyCalculationResult> CalculatePolicyAsync(int studentCount, decimal originalFee);

    /// <summary>
    /// Calculate per-student stacked discounts with optional existing count and fee per student.
    /// </summary>
    /// <param name="studentCount">Number of students in current registration (>=1)</param>
    /// <param name="existingCount">Number of students already active/registered for same pickup</param>
    /// <param name="originalFeePerStudent">Optional original fee per student to compute amounts</param>
    Task<CalculatePerStudentResponse> CalculatePerStudentAsync(int studentCount, int existingCount, decimal? originalFeePerStudent = null);

    /// <summary>
    /// Create new policy
    /// </summary>
    Task<MultiStudentPolicyResponseDto> CreatePolicyAsync(CreateMultiStudentPolicyDto dto);

    /// <summary>
    /// Update policy
    /// </summary>
    Task<MultiStudentPolicyResponseDto?> UpdatePolicyAsync(UpdateMultiStudentPolicyDto dto);

    /// <summary>
    /// Delete policy (soft delete)
    /// </summary>
    Task<bool> DeletePolicyAsync(Guid id);

    /// <summary>
    /// Activate policy
    /// </summary>
    Task<bool> ActivatePolicyAsync(Guid id);

    /// <summary>
    /// Deactivate policy
    /// </summary>
    Task<bool> DeactivatePolicyAsync(Guid id);
}

