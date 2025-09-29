using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.UnitPrice;
using System.Security.Claims;
using Constants;

namespace APIs.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Admin)]
public class UnitPriceController : ControllerBase
{
    private readonly IUnitPriceService _unitPriceService;

    public UnitPriceController(IUnitPriceService unitPriceService)
    {
        _unitPriceService = unitPriceService;
    }

    private (Guid adminId, string adminName) GetAdminInfo()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        var userEmailClaim = User.FindFirst(ClaimTypes.Email);
        
        if (userIdClaim == null || userEmailClaim == null)
        {
            throw new UnauthorizedAccessException("Unable to retrieve admin information from token.");
        }

        if (!Guid.TryParse(userIdClaim.Value, out var adminId))
        {
            throw new UnauthorizedAccessException("Invalid admin ID.");
        }

        return (adminId, userEmailClaim.Value);
    }

    [HttpGet]
    public async Task<ActionResult<List<UnitPriceResponseDto>>> GetAllUnitPrices()
    {
        var unitPrices = await _unitPriceService.GetAllUnitPricesAsync();
        return Ok(unitPrices);
    }

    [HttpGet("all-including-deleted")]
    public async Task<ActionResult<List<UnitPriceResponseDto>>> GetAllIncludingDeleted()
    {
        var unitPrices = await _unitPriceService.GetAllIncludingDeletedAsync();
        return Ok(unitPrices);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UnitPriceResponseDto>> GetUnitPrice(Guid id)
    {
        var unitPrice = await _unitPriceService.GetUnitPriceByIdAsync(id);
        if (unitPrice == null) return NotFound();
        return Ok(unitPrice);
    }

    [HttpGet("active")]
    public async Task<ActionResult<List<UnitPriceResponseDto>>> GetActiveUnitPrices()
    {
        var unitPrices = await _unitPriceService.GetActiveUnitPricesAsync();
        return Ok(unitPrices);
    }

    [HttpGet("effective")]
    public async Task<ActionResult<List<UnitPriceResponseDto>>> GetEffectiveUnitPrices([FromQuery] DateTime? date = null)
    {
        var unitPrices = await _unitPriceService.GetEffectiveUnitPricesAsync(date);
        return Ok(unitPrices);
    }

    [HttpGet("current-effective")]
    [AllowAnonymous]
    public async Task<ActionResult<UnitPriceResponseDto>> GetCurrentEffectiveUnitPrice()
    {
        var unitPrice = await _unitPriceService.GetCurrentEffectiveUnitPriceAsync();
        if (unitPrice == null) return NotFound("No effective unit price found.");
        return Ok(unitPrice);
    }

    [HttpPost]
    public async Task<ActionResult<UnitPriceResponseDto>> CreateUnitPrice([FromBody] CreateUnitPriceDto dto)
    {
        try
        {
            var (adminId, adminName) = GetAdminInfo();
            dto.ByAdminId = adminId;
            dto.ByAdminName = adminName;

            var unitPrice = await _unitPriceService.CreateUnitPriceAsync(dto);
            return CreatedAtAction(nameof(GetUnitPrice), new { id = unitPrice.Id }, unitPrice);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error creating UnitPrice: {ex.Message}");
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UnitPriceResponseDto>> UpdateUnitPrice(Guid id, [FromBody] UpdateUnitPriceDto dto)
    {
        if (id != dto.Id) return BadRequest("ID mismatch");

        var unitPrice = await _unitPriceService.UpdateUnitPriceAsync(dto);
        if (unitPrice == null) return NotFound();

        return Ok(unitPrice);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteUnitPrice(Guid id)
    {
        var result = await _unitPriceService.DeleteUnitPriceAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpPut("{id}/activate")]
    public async Task<ActionResult> ActivateUnitPrice(Guid id)
    {
        var result = await _unitPriceService.ActivateUnitPriceAsync(id);
        if (!result) return NotFound();
        return Ok();
    }

    [HttpPut("{id}/deactivate")]
    public async Task<ActionResult> DeactivateUnitPrice(Guid id)
    {
        var result = await _unitPriceService.DeactivateUnitPriceAsync(id);
        if (!result) return NotFound();
        return Ok();
    }
}
