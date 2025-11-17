using Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.StudentAbsenceRequest;
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

            var student = await _studentService.GetStudentByIdAsync(request.StudentId);
            if (student is null)
                return NotFound(new { message = "Student not found." });

            if (!student.ParentId.HasValue)
                return BadRequest(new { message = "Student has not been assigned to a parent yet." });

            if (!AuthorizationHelper.CanAccessStudentData(HttpContext, student.ParentId))
                return Forbid();

            var parentId = student.ParentId.Value;

            request.ParentId = parentId;

            try
            {
                var created = await _absenceService.CreateAsync(request);
                return CreatedAtAction(nameof(GetByStudent), new { studentId = created.StudentId }, created);
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
        [HttpGet("students/{studentId:guid}")]
        public async Task<IActionResult> GetByStudent(Guid studentId)
        {
            var student = await _studentService.GetStudentByIdAsync(studentId);
            if (student is null)
                return NotFound(new { message = "Student not found." });

            if (!AuthorizationHelper.CanAccessStudentData(HttpContext, student.ParentId))
                return Forbid();

            var requests = await _absenceService.GetByStudentAsync(studentId);
            return Ok(requests);
        }

        [Authorize(Roles = $"{Roles.Admin},{Roles.Parent}")]
        [HttpGet("parents/{parentId:guid}")]
        public async Task<IActionResult> GetByParent(Guid parentId)
        {
            if (!AuthorizationHelper.CanAccessParentData(HttpContext, parentId))
                return Forbid();

            var requests = await _absenceService.GetByParentAsync(parentId);
            return Ok(requests);
        }

    }
}

