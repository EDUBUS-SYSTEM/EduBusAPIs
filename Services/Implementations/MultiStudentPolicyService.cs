using AutoMapper;
using Data.Models;
using Data.Repos.Interfaces;
using Services.Contracts;
using Services.Models.MultiStudentPolicy;

namespace Services.Implementations;

public class MultiStudentPolicyService : IMultiStudentPolicyService
{
    private readonly IMultiStudentPolicyRepository _policyRepository;
    private readonly IMapper _mapper;

    public MultiStudentPolicyService(
        IMultiStudentPolicyRepository policyRepository,
        IMapper mapper)
    {
        _policyRepository = policyRepository;
        _mapper = mapper;
    }

    public async Task<List<MultiStudentPolicyResponseDto>> GetAllPoliciesAsync()
    {
        var policies = await _policyRepository.FindAllAsync();
        return _mapper.Map<List<MultiStudentPolicyResponseDto>>(policies);
    }

    public async Task<List<MultiStudentPolicyResponseDto>> GetAllIncludingDeletedAsync()
    {
        var policies = await _policyRepository.GetAllIncludingDeletedAsync();
        return _mapper.Map<List<MultiStudentPolicyResponseDto>>(policies);
    }

    public async Task<MultiStudentPolicyResponseDto?> GetPolicyByIdAsync(Guid id)
    {
        var policy = await _policyRepository.FindAsync(id);
        return policy == null ? null : _mapper.Map<MultiStudentPolicyResponseDto>(policy);
    }

    public async Task<MultiStudentPolicyResponseDto?> GetCurrentActivePolicyAsync()
    {
        var policy = await _policyRepository.GetCurrentActivePolicyAsync();
        return policy == null ? null : _mapper.Map<MultiStudentPolicyResponseDto>(policy);
    }

    public async Task<PolicyCalculationResult> CalculatePolicyAsync(int studentCount, decimal originalFee)
    {
        // If only 1 student, no policy applies
        if (studentCount < 2)
        {
            return new PolicyCalculationResult
            {
                ReductionAmount = 0,
                ReductionPercentage = 0,
                Description = "Policy not applicable (only 1 student)",
                AppliedTierId = null
            };
        }

        // Get current active policy
        var policy = await _policyRepository.GetCurrentActivePolicyAsync();
        
        if (policy == null || !policy.IsActive)
        {
            return new PolicyCalculationResult
            {
                ReductionAmount = 0,
                ReductionPercentage = 0,
                Description = "No active policy found",
                AppliedTierId = null
            };
        }

        // Find applicable tier (filter out deleted tiers)
        var applicableTier = policy.Tiers
            .Where(t => !t.IsDeleted &&
                       studentCount >= t.MinStudentCount &&
                       (t.MaxStudentCount == null || studentCount <= t.MaxStudentCount))
            .OrderByDescending(t => t.MinStudentCount) // Prioritize tier with highest MinStudentCount
            .FirstOrDefault();

        if (applicableTier == null)
        {
            return new PolicyCalculationResult
            {
                ReductionAmount = 0,
                ReductionPercentage = 0,
                Description = $"No applicable tier found for {studentCount} students",
                AppliedTierId = null
            };
        }

        // Calculate reduction amount
        var reductionAmount = originalFee * (applicableTier.ReductionPercentage / 100m);

        return new PolicyCalculationResult
        {
            ReductionAmount = reductionAmount,
            ReductionPercentage = applicableTier.ReductionPercentage,
            Description = applicableTier.Description ?? 
                         $"{applicableTier.ReductionPercentage}% reduction for {studentCount} students",
            AppliedTierId = applicableTier.Id
        };
    }

    public async Task<CalculatePerStudentResponse> CalculatePerStudentAsync(int studentCount, int existingCount, decimal? originalFeePerStudent = null)
    {
        var response = new CalculatePerStudentResponse
        {
            MaxDiscountCap = 100m
        };

        // Guard: invalid student count
        if (studentCount < 1)
        {
            return response;
        }

        // Get current active policy
        var policy = await _policyRepository.GetCurrentActivePolicyAsync();
        if (policy == null || !policy.IsActive)
        {
            // No policy -> zero discounts
            for (int i = 0; i < studentCount; i++)
            {
                var original = originalFeePerStudent ?? 0;
                response.Students.Add(new PerStudentDiscountResult
                {
                    Position = existingCount + i + 1,
                    DiscountPercentage = 0,
                    DiscountAmount = 0,
                    OriginalFee = original,
                    FinalFee = original,
                    Description = "No active policy"
                });
            }
            return response;
        }

        response.PolicyId = policy.Id;
        response.PolicyName = policy.Name;

        // Determine total count to select tier (existing + new)
        var totalCount = existingCount + studentCount;
        if (totalCount < 2)
        {
            // Still not eligible
            for (int i = 0; i < studentCount; i++)
            {
                var original = originalFeePerStudent ?? 0;
                response.Students.Add(new PerStudentDiscountResult
                {
                    Position = existingCount + i + 1,
                    DiscountPercentage = 0,
                    DiscountAmount = 0,
                    OriginalFee = original,
                    FinalFee = original,
                    Description = "Policy not applicable (only 1 student)"
                });
            }
            return response;
        }

        var applicableTier = policy.Tiers
            .Where(t => !t.IsDeleted &&
                       totalCount >= t.MinStudentCount &&
                       (t.MaxStudentCount == null || totalCount <= t.MaxStudentCount))
            .OrderByDescending(t => t.MinStudentCount)
            .FirstOrDefault();

        if (applicableTier == null)
        {
            for (int i = 0; i < studentCount; i++)
            {
                var original = originalFeePerStudent ?? 0;
                response.Students.Add(new PerStudentDiscountResult
                {
                    Position = existingCount + i + 1,
                    DiscountPercentage = 0,
                    DiscountAmount = 0,
                    OriginalFee = original,
                    FinalFee = original,
                    Description = $"No applicable tier found for {totalCount} students"
                });
            }
            return response;
        }

        response.BasePercentage = applicableTier.ReductionPercentage;

        // Discount rule:
        // - If existingCount == 0 (register many at once): each student gets basePct.
        // - If existingCount > 0 (register later): each new student still gets basePct of the applicable tier (no stacking multiplier),
        //   tier selection still uses (existing + new) to move to higher tier if applicable.
        var appliedPct = applicableTier.ReductionPercentage;
        if (appliedPct > response.MaxDiscountCap) appliedPct = response.MaxDiscountCap;

        for (int i = 0; i < studentCount; i++)
        {
            var pos = existingCount + i + 1;
            var pct = appliedPct;
            var original = originalFeePerStudent ?? 0;
            var discountAmount = original * (pct / 100m);
            var finalFee = original - discountAmount;

            response.Students.Add(new PerStudentDiscountResult
            {
                Position = pos,
                DiscountPercentage = pct,
                DiscountAmount = originalFeePerStudent.HasValue ? discountAmount : 0,
                OriginalFee = original,
                FinalFee = originalFeePerStudent.HasValue ? finalFee : original,
                AppliedTierId = applicableTier.Id,
                Description = existingCount == 0
                    ? $"{pct}% discount per student (same-time registration)"
                    : $"{pct}% discount for additional registration at same pickup point"
            });
        }

        return response;
    }

    public async Task<MultiStudentPolicyResponseDto> CreatePolicyAsync(CreateMultiStudentPolicyDto dto)
    {
        // Validate tiers
        ValidateTiers(dto.Tiers);

        // Check overlap with existing active policies
        await ValidateNoOverlappingActivePolicies(dto.EffectiveFrom, dto.EffectiveTo);

        // Create policy document with embedded tiers
        var policy = new MultiStudentPolicyDocument
        {
            Name = dto.Name,
            Description = dto.Description,
            IsActive = true,
            EffectiveFrom = dto.EffectiveFrom,
            EffectiveTo = dto.EffectiveTo,
            ByAdminId = dto.ByAdminId,
            ByAdminName = dto.ByAdminName,
            Tiers = dto.Tiers.Select(t => new MultiStudentPolicyTierDocument
            {
                MinStudentCount = t.MinStudentCount,
                MaxStudentCount = t.MaxStudentCount,
                ReductionPercentage = t.ReductionPercentage,
                Description = t.Description,
                Priority = t.Priority,
                CreatedAt = DateTime.UtcNow
            }).ToList()
        };

        await _policyRepository.AddAsync(policy);
        return _mapper.Map<MultiStudentPolicyResponseDto>(policy);
    }

    public async Task<MultiStudentPolicyResponseDto?> UpdatePolicyAsync(UpdateMultiStudentPolicyDto dto)
    {
        var policy = await _policyRepository.FindAsync(dto.Id);
        if (policy == null)
            return null;

        // Validate tiers
        ValidateTiers(dto.Tiers.Select(t => new CreatePolicyTierDto
        {
            MinStudentCount = t.MinStudentCount,
            MaxStudentCount = t.MaxStudentCount,
            ReductionPercentage = t.ReductionPercentage,
            Description = t.Description,
            Priority = t.Priority
        }).ToList());

        // Check overlap (excluding this policy)
        await ValidateNoOverlappingActivePolicies(dto.EffectiveFrom, dto.EffectiveTo, dto.Id);

        // Update policy
        policy.Name = dto.Name;
        policy.Description = dto.Description;
        policy.IsActive = dto.IsActive;
        policy.EffectiveFrom = dto.EffectiveFrom;
        policy.EffectiveTo = dto.EffectiveTo;
        policy.ByAdminId = dto.ByAdminId;
        policy.ByAdminName = dto.ByAdminName;

        // Update tiers (embedded documents)
        var existingTierIds = dto.Tiers
            .Where(t => t.Id.HasValue)
            .Select(t => t.Id!.Value)
            .ToList();

        // Mark tiers not in the list as deleted
        foreach (var tier in policy.Tiers)
        {
            if (!existingTierIds.Contains(tier.Id))
            {
                tier.IsDeleted = true;
            }
        }

        // Update or create tiers
        foreach (var tierDto in dto.Tiers)
        {
            if (tierDto.Id.HasValue)
            {
                // Update existing tier
                var existingTier = policy.Tiers.FirstOrDefault(t => t.Id == tierDto.Id.Value);
                if (existingTier != null)
                {
                    existingTier.MinStudentCount = tierDto.MinStudentCount;
                    existingTier.MaxStudentCount = tierDto.MaxStudentCount;
                    existingTier.ReductionPercentage = tierDto.ReductionPercentage;
                    existingTier.Description = tierDto.Description;
                    existingTier.Priority = tierDto.Priority;
                    existingTier.IsDeleted = false; // Re-activate if was deleted
                }
            }
            else
            {
                // Create new tier
                policy.Tiers.Add(new MultiStudentPolicyTierDocument
                {
                    MinStudentCount = tierDto.MinStudentCount,
                    MaxStudentCount = tierDto.MaxStudentCount,
                    ReductionPercentage = tierDto.ReductionPercentage,
                    Description = tierDto.Description,
                    Priority = tierDto.Priority,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        // Sort tiers by priority
        policy.Tiers = policy.Tiers.OrderBy(t => t.Priority).ToList();

        await _policyRepository.UpdateAsync(policy);
        return _mapper.Map<MultiStudentPolicyResponseDto>(policy);
    }

    public async Task<bool> DeletePolicyAsync(Guid id)
    {
        var policy = await _policyRepository.FindAsync(id);
        if (policy == null)
            return false;

        // Soft delete policy (tiers are embedded, so they're deleted with the policy)
        await _policyRepository.DeleteAsync(id);
        return true;
    }

    public async Task<bool> ActivatePolicyAsync(Guid id)
    {
        var policy = await _policyRepository.FindAsync(id);
        if (policy == null)
            return false;

        policy.IsActive = true;
        policy.UpdatedAt = DateTime.UtcNow;
        await _policyRepository.UpdateAsync(policy);
        return true;
    }

    public async Task<bool> DeactivatePolicyAsync(Guid id)
    {
        var policy = await _policyRepository.FindAsync(id);
        if (policy == null)
            return false;

        policy.IsActive = false;
        policy.UpdatedAt = DateTime.UtcNow;
        await _policyRepository.UpdateAsync(policy);
        return true;
    }

    private void ValidateTiers(List<CreatePolicyTierDto> tiers)
    {
        if (tiers == null || tiers.Count == 0)
            throw new ArgumentException("At least 1 tier is required");

        // Check priority uniqueness
        var priorities = tiers.Select(t => t.Priority).ToList();
        if (priorities.Distinct().Count() != priorities.Count)
            throw new ArgumentException("Priority values must be unique");

        // Check min student count >= 2
        if (tiers.Any(t => t.MinStudentCount < 2))
            throw new ArgumentException("Minimum student count must be at least 2");

        // Check max >= min
        foreach (var tier in tiers)
        {
            if (tier.MaxStudentCount.HasValue && tier.MaxStudentCount < tier.MinStudentCount)
                throw new ArgumentException($"Maximum student count must be greater than or equal to minimum student count (Tier: {tier.MinStudentCount})");
        }

        // Check overlap ranges
        var sortedTiers = tiers.OrderBy(t => t.MinStudentCount).ToList();
        for (int i = 0; i < sortedTiers.Count - 1; i++)
        {
            var current = sortedTiers[i];
            var next = sortedTiers[i + 1];

            var currentMax = current.MaxStudentCount ?? int.MaxValue;
            var nextMin = next.MinStudentCount;

            if (currentMax >= nextMin)
            {
                throw new ArgumentException(
                    $"Tier ranges cannot overlap. " +
                    $"Tier {current.MinStudentCount}-{current.MaxStudentCount} and " +
                    $"Tier {next.MinStudentCount}-{next.MaxStudentCount} overlap.");
            }
        }
    }

    private async Task ValidateNoOverlappingActivePolicies(DateTime effectiveFrom, DateTime? effectiveTo, Guid? excludePolicyId = null)
    {
        var existingPolicies = await _policyRepository.GetActivePoliciesAsync();
        
        if (excludePolicyId.HasValue)
        {
            existingPolicies = existingPolicies.Where(p => p.Id != excludePolicyId.Value).ToList();
        }

        var newTo = effectiveTo ?? DateTime.MaxValue;

        foreach (var existingPolicy in existingPolicies)
        {
            var existingFrom = existingPolicy.EffectiveFrom;
            var existingTo = existingPolicy.EffectiveTo ?? DateTime.MaxValue;

            // Check overlap: (newFrom < existingTo) && (newTo > existingFrom)
            if (effectiveFrom < existingTo && newTo > existingFrom)
            {
                throw new InvalidOperationException(
                    $"Policy '{existingPolicy.Name}' already exists in the time range " +
                    $"from {existingFrom:dd/MM/yyyy} to {(existingPolicy.EffectiveTo?.ToString("dd/MM/yyyy") ?? "no limit")}. " +
                    "Cannot create new policy in this time range.");
            }
        }
    }
}

