using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.School;
using Constants;
using System.Collections.Generic;

namespace APIs.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SchoolController : ControllerBase
{
    private readonly ISchoolService _schoolService;

    public SchoolController(ISchoolService schoolService)
    {
        _schoolService = schoolService;
    }

    /// <summary>
    /// Get school information (public endpoint for homepage/mobile)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SchoolDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSchool()
    {
        var school = await _schoolService.GetSchoolAsync();
        if (school == null)
            return NotFound(new { message = "School information not found" });

        return Ok(school);
    }

    /// <summary>
    /// Get school information with internal notes (admin only)
    /// Returns empty school if not found (to allow creation)
    /// </summary>
    [HttpGet("admin")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(SchoolDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSchoolForAdmin()
    {
        var school = await _schoolService.GetSchoolForAdminAsync();
        
        // Return empty school if not found (allows frontend to show create form)
        if (school == null)
        {
            return Ok(new SchoolDto
            {
                Id = Guid.Empty,
                SchoolName = "",
                Slogan = "",
                ShortDescription = "",
                FullDescription = "",
                Email = "",
                PhoneNumber = "",
                Website = "",
                FullAddress = "",
                DisplayAddress = "",
                Latitude = null,
                Longitude = null,
                FooterText = "",
                GalleryImages = new List<SchoolImageContentDto>(),
                IsPublished = false,
                IsActive = false,
                InternalNotes = "",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            });
        }

        return Ok(school);
    }

    /// <summary>
    /// Create school information (admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(SchoolDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateSchool([FromBody] CreateSchoolRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var school = await _schoolService.CreateSchoolAsync(request);
            return CreatedAtAction(nameof(GetSchoolForAdmin), new { }, school);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Update school information (admin only)
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(SchoolDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSchool([FromRoute] Guid id, [FromBody] UpdateSchoolRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var school = await _schoolService.UpdateSchoolAsync(id, request);
            return Ok(school);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Update school location from map picker (admin only)
    /// </summary>
    [HttpPut("{id:guid}/location")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(SchoolDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSchoolLocation([FromRoute] Guid id, [FromBody] SchoolLocationRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var school = await _schoolService.UpdateSchoolLocationAsync(id, request);
            return Ok(school);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Get school images (Logo, Banner, Gallery) - admin only
    /// </summary>
    [HttpGet("images")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(List<SchoolImageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSchoolImages()
    {
        try
        {
            var images = await _schoolService.GetSchoolImagesAsync();
            return Ok(images);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }
}

