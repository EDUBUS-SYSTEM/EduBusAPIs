using Constants;
using Data.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.StudentAbsenceRequest;
using System.Security.Claims;
using Utils;
using System;

namespace APIs.Controllers
{
    [Route("api/student-absence-requests")]
    [ApiController]
    [Authorize]
    public class StudentAbsenceRequestController : ControllerBase
    {
        private readonly IStudentAbsenceRequestService _absenceService;
        private readonly IStudentService _studentService;

        public StudentAbsenceRequestController(
            IStudentAbsenceRequestService absenceService,
            IStudentService studentService)
        {
            _absenceService = absenceService;
            _studentService = studentService;
        }

        [Authorize(Roles = Roles.Parent)]
        [HttpPost("parent")]
        public async Task<IActionResult> Create([FromBody] CreateStudentAbsenceRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var created = await _absenceService.CreateAsync(request, HttpContext);
                return CreatedAtAction(nameof(GetByStudent), new { studentId = created.StudentId }, created);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }

        [Authorize(Roles = $"{Roles.Admin},{Roles.Parent}")]
        [HttpGet("{requestId:guid}")]
        public async Task<IActionResult> GetById(Guid requestId)
        {
            var request = await _absenceService.GetByIdAsync(requestId);
            if (request is null)
                return NotFound(new { message = "Absence request not found." });

            if (!User.IsInRole(Roles.Admin) && !AuthorizationHelper.CanAccessParentData(HttpContext, request.ParentId))
                return Forbid();

            return Ok(request);
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] AbsenceRequestStatus? status = null,
            [FromQuery] string? search = null,
            [FromQuery] CreateAtSortOption sort = CreateAtSortOption.Newest,
            [FromQuery] int page = 1,
            [FromQuery] int perPage = 20)
        {
            var requests = await _absenceService.GetAllAsync(startDate, endDate, status, search, sort, page, perPage);
            return Ok(requests);
        }

        [Authorize(Roles = $"{Roles.Admin},{Roles.Parent}")]
        [HttpGet("students/{studentId:guid}")]
        public async Task<IActionResult> GetByStudent(
            Guid studentId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] AbsenceRequestStatus? status = null,
            [FromQuery] CreateAtSortOption sort = CreateAtSortOption.Newest,
            [FromQuery] int page = 1,
            [FromQuery] int perPage = 20)
        {
            var student = await _studentService.GetStudentByIdAsync(studentId);
            if (student is null)
                return NotFound(new { message = "Student not found." });

            if (!AuthorizationHelper.CanAccessStudentData(HttpContext, student.ParentId))
                return Forbid();

            var requests = await _absenceService.GetByStudentAsync(studentId, startDate, endDate, status, sort, page, perPage);
            return Ok(requests);
        }

        [Authorize(Roles = Roles.Parent)]
        [HttpGet("parents/me")]
        public async Task<IActionResult> GetByParent(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] AbsenceRequestStatus? status = null,
            [FromQuery] CreateAtSortOption sort = CreateAtSortOption.Newest,
            [FromQuery] int page = 1,
            [FromQuery] int perPage = 20)
        {
            var currentParentId = AuthorizationHelper.GetCurrentUserId(HttpContext);
            if (!currentParentId.HasValue)
                return Unauthorized(new { message = "Parent ID not found." });

            var requests = await _absenceService.GetByParentAsync(currentParentId.Value, startDate, endDate, status, sort, page, perPage);
            return Ok(requests);
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpPatch("{requestId:guid}/reject")]
        public async Task<IActionResult> RejectRequest(Guid requestId, [FromBody] RejectStudentAbsenceRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (adminIdClaim == null)
                    return Unauthorized(new { message = "Admin ID not found." });

                Guid adminId = Guid.Parse(adminIdClaim.Value);
                var rejected = await _absenceService.RejectRequestAsync(requestId, request, adminId);
                return Ok(rejected);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("not found"))
                    return NotFound(new { message = ex.Message });
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while rejecting absence request.", detail = ex.Message });
            }
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpPatch("{requestId:guid}/approve")]
        public async Task<IActionResult> ApproveRequest(Guid requestId, [FromBody] ApproveStudentAbsenceRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (adminIdClaim == null)
                    return Unauthorized(new { message = "Admin ID not found." });

                Guid adminId = Guid.Parse(adminIdClaim.Value);
                var approved = await _absenceService.ApproveRequestAsync(requestId, request, adminId);
                return Ok(approved);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                    return NotFound(new { message = ex.Message });
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while approving absence request.", detail = ex.Message });
            }
        }
    }
}

