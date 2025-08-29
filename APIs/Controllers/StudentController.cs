using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Implementations;
using Services.Models.Student;
using Microsoft.AspNetCore.Authorization;
using Constants;

namespace APIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentController : ControllerBase
    {
        private readonly IStudentService _studentService;

        public StudentController(IStudentService studentService)
        {
            _studentService = studentService;
        }

        private bool IsAuthorizedToAccessStudent(Guid studentParentId)
        {
            var isAdmin = User.IsInRole(Roles.Admin);
            if (isAdmin) return true;

            var parentIdClaim = User.FindFirst("ParentId")?.Value;
            if (string.IsNullOrEmpty(parentIdClaim) || !Guid.TryParse(parentIdClaim, out var currentParentId))
                return false;

            return studentParentId == currentParentId;
        }

        private bool IsAuthorizedToAccessParent(Guid parentId)
        {
            var isAdmin = User.IsInRole(Roles.Admin);
            if (isAdmin) return true;

            var parentIdClaim = User.FindFirst("ParentId")?.Value;
            if (string.IsNullOrEmpty(parentIdClaim) || !Guid.TryParse(parentIdClaim, out var currentParentId))
                return false;

            return parentId == currentParentId;
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpPost]
        public async Task<IActionResult> CreateStudent([FromBody] CreateStudentRequest request)
        {
            try
            {
                var student = await _studentService.CreateStudentAsync(request);
                return CreatedAtAction(nameof(GetStudentById), new { id = student.Id }, student);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
            
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStudent(Guid id, [FromBody] UpdateStudentRequest request)
        {
            try
            {
                var student = await _studentService.UpdateStudentAsync(id, request);
                return Ok(student);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
            
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpGet]
        public async Task<IActionResult> GetAllStudents()
        {
            var students = await _studentService.GetAllStudentsAsync();
            return Ok(students);
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetStudentById(Guid id)
        {
            var student = await _studentService.GetStudentByIdAsync(id);
            if (student == null)
                return NotFound();

            // Check authorization - only allow access if student has a parent and user is authorized
            if (student.ParentId.HasValue)
            {
                if (!IsAuthorizedToAccessStudent(student.ParentId.Value))
                {
                    return Forbid();
                }
            }
            else
            {
                // Student without parent - only admin can access
                if (!User.IsInRole(Roles.Admin))
                {
                    return Forbid();
                }
            }

            return Ok(student);
        }

        [Authorize]
        [HttpGet("parent/{parentId}")]
        public async Task<IActionResult> GetStudentsByParent(Guid parentId)
        {
            // Check authorization
            if (!IsAuthorizedToAccessParent(parentId))
            {
                return Forbid();
            }

            var students = await _studentService.GetStudentsByParentAsync(parentId);
            return Ok(students);
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpPost("import")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ImportStudents(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is required.");

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Invalid file format. Only .xlsx files are supported.");

            try
            {
                using var stream = file.OpenReadStream();
                var result = await _studentService.ImportStudentsFromExcelAsync(stream);

                return Ok(new
                {
                    TotalProcessed = result.TotalProcessed,
                    SuccessfulStudents = result.SuccessfulStudents,
                    FailedStudents = result.FailedStudents
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while importing students.",
                    Details = ex.Message
                });
            }
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpGet("export")]
        public async Task<IActionResult> ExportStudents()
        {
            var fileContent = await _studentService.ExportStudentsToExcelAsync();
            if (fileContent == null || fileContent.Length == 0)
            {
                return NotFound(new { message = "No data to export to Excel." });
            }

            return File(fileContent,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "Students.xlsx");
        }
    }
}
