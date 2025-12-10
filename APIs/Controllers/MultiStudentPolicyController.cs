using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.MultiStudentPolicy;
using Constants;

namespace APIs.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Admin)]
public class MultiStudentPolicyController : ControllerBase
{
    private readonly IMultiStudentPolicyService _policyService;

    public MultiStudentPolicyController(IMultiStudentPolicyService policyService)
    {
        _policyService = policyService;
    }

    /// <summary>
    /// Get all policies
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllPolicies()
    {
        var policies = await _policyService.GetAllPoliciesAsync();
        return Ok(new
        {
            success = true,
            data = policies,
            error = (object?)null
        });
    }

    /// <summary>
    /// Get all policies (including deleted)
    /// </summary>
    [HttpGet("all-including-deleted")]
    public async Task<IActionResult> GetAllIncludingDeleted()
    {
        var policies = await _policyService.GetAllIncludingDeletedAsync();
        return Ok(new
        {
            success = true,
            data = policies,
            error = (object?)null
        });
    }

    /// <summary>
    /// Get policy by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPolicyById(Guid id)
    {
        var policy = await _policyService.GetPolicyByIdAsync(id);
        if (policy == null)
        {
            return NotFound(new
            {
                success = false,
                data = (object?)null,
                error = new { code = "POLICY_NOT_FOUND", message = "Policy not found" }
            });
        }

        return Ok(new
        {
            success = true,
            data = policy,
            error = (object?)null
        });
    }

    /// <summary>
    /// Get current active policy
    /// </summary>
    [HttpGet("current-active")]
    [AllowAnonymous] // Allow public access for frontend fee calculation
    public async Task<IActionResult> GetCurrentActivePolicy()
    {
        var policy = await _policyService.GetCurrentActivePolicyAsync();
        return Ok(new
        {
            success = true,
            data = policy,
            error = (object?)null
        });
    }

    /// <summary>
    /// Calculate policy reduction based on student count and original fee
    /// </summary>
    [HttpPost("calculate")]
    [AllowAnonymous] // Allow public access for frontend fee calculation
    public async Task<IActionResult> CalculatePolicy([FromBody] CalculatePolicyRequest request)
    {
        if (request.StudentCount < 1)
        {
            return BadRequest(new
            {
                success = false,
                data = (object?)null,
                error = new { code = "INVALID_STUDENT_COUNT", message = "Student count must be greater than 0" }
            });
        }

        if (request.OriginalFee < 0)
        {
            return BadRequest(new
            {
                success = false,
                data = (object?)null,
                error = new { code = "INVALID_FEE", message = "Original fee must be greater than or equal to 0" }
            });
        }

        var result = await _policyService.CalculatePolicyAsync(request.StudentCount, request.OriginalFee);
        return Ok(new
        {
            success = true,
            data = result,
            error = (object?)null
        });
    }

    /// <summary>
    /// Create new policy
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreatePolicy([FromBody] CreateMultiStudentPolicyDto dto)
    {
        try
        {
            var policy = await _policyService.CreatePolicyAsync(dto);
            return Ok(new
            {
                success = true,
                data = policy,
                error = (object?)null
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                success = false,
                data = (object?)null,
                error = new { code = "VALIDATION_ERROR", message = ex.Message }
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                success = false,
                data = (object?)null,
                error = new { code = "CONFLICT", message = ex.Message }
            });
        }
    }

    /// <summary>
    /// Update policy
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> UpdatePolicy([FromBody] UpdateMultiStudentPolicyDto dto)
    {
        try
        {
            var policy = await _policyService.UpdatePolicyAsync(dto);
            if (policy == null)
            {
                return NotFound(new
                {
                    success = false,
                    data = (object?)null,
                    error = new { code = "POLICY_NOT_FOUND", message = "Policy not found" }
                });
            }

            return Ok(new
            {
                success = true,
                data = policy,
                error = (object?)null
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                success = false,
                data = (object?)null,
                error = new { code = "VALIDATION_ERROR", message = ex.Message }
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                success = false,
                data = (object?)null,
                error = new { code = "CONFLICT", message = ex.Message }
            });
        }
    }

    /// <summary>
    /// Delete policy (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePolicy(Guid id)
    {
        var result = await _policyService.DeletePolicyAsync(id);
        if (!result)
        {
            return NotFound(new
            {
                success = false,
                data = (object?)null,
                error = new { code = "POLICY_NOT_FOUND", message = "Policy not found" }
            });
        }

        return Ok(new
        {
            success = true,
            data = new { message = "Policy deleted successfully" },
            error = (object?)null
        });
    }

    /// <summary>
    /// Activate policy
    /// </summary>
    [HttpPost("{id}/activate")]
    public async Task<IActionResult> ActivatePolicy(Guid id)
    {
        var result = await _policyService.ActivatePolicyAsync(id);
        if (!result)
        {
            return NotFound(new
            {
                success = false,
                data = (object?)null,
                error = new { code = "POLICY_NOT_FOUND", message = "Policy not found" }
            });
        }

        return Ok(new
        {
            success = true,
            data = new { message = "Policy activated successfully" },
            error = (object?)null
        });
    }

    /// <summary>
    /// Deactivate policy
    /// </summary>
    [HttpPost("{id}/deactivate")]
    public async Task<IActionResult> DeactivatePolicy(Guid id)
    {
        var result = await _policyService.DeactivatePolicyAsync(id);
        if (!result)
        {
            return NotFound(new
            {
                success = false,
                data = (object?)null,
                error = new { code = "POLICY_NOT_FOUND", message = "Policy not found" }
            });
        }

        return Ok(new
        {
            success = true,
            data = new { message = "Policy deactivated successfully" },
            error = (object?)null
        });
    }

    /// <summary>
    /// Calculate per-student stacked discounts (optional fee input).
    /// </summary>
    [HttpPost("calculate-per-student")]
    [AllowAnonymous]
    public async Task<IActionResult> CalculatePerStudent([FromBody] CalculatePerStudentRequest request)
    {
        if (request.StudentCount < 1)
        {
            return BadRequest(new
            {
                success = false,
                data = (object?)null,
                error = new { code = "INVALID_STUDENT_COUNT", message = "Student count must be greater than 0" }
            });
        }

        var result = await _policyService.CalculatePerStudentAsync(request.StudentCount, request.ExistingCount, request.OriginalFeePerStudent);
        return Ok(new
        {
            success = true,
            data = result,
            error = (object?)null
        });
    }
}

/// <summary>
/// Request DTO for policy calculation
/// </summary>
public class CalculatePolicyRequest
{
    public int StudentCount { get; set; }
    public decimal OriginalFee { get; set; }
}

