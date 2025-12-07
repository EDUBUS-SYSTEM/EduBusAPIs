using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.Student;
using Microsoft.AspNetCore.Authorization;
using Constants;
using System.Security.Claims;
using Utils;

namespace APIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentController : ControllerBase
    {
        private readonly IStudentService _studentService;
        private readonly IFileService _fileService;

        public StudentController(IStudentService studentService, IFileService fileService)
        {
            _studentService = studentService;
            _fileService = fileService;
        }

        private bool IsAuthorizedToAccessStudent(Guid studentParentId)
        {
            var isAdmin = User.IsInRole(Roles.Admin);
            if (isAdmin) return true;

            var parentIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(parentIdClaim) || !Guid.TryParse(parentIdClaim, out var currentParentId))
                return false;

            return studentParentId == currentParentId;
        }

        private bool IsAuthorizedToAccessParent(Guid parentId)
        {
            var isAdmin = User.IsInRole(Roles.Admin);
            if (isAdmin) return true;

            var parentIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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

            // Check authorization using the optimized helper method
            if (!AuthorizationHelper.CanAccessStudentData(Request.HttpContext, student.ParentId))
            {
                return Forbid();
            }

            return Ok(student);
        }

        [Authorize]
        [HttpGet("parent/{parentId}")]
        public async Task<IActionResult> GetStudentsByParent(Guid parentId)
        {
            // Check authorization using the optimized helper method
            if (!AuthorizationHelper.CanAccessParentData(Request.HttpContext, parentId))
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

        [Authorize(Roles = Roles.Admin)]
        [HttpPost("{id}/activate")]
        public async Task<IActionResult> ActivateStudent(Guid id)
        {
            try
            {
                var student = await _studentService.ActivateStudentAsync(id);
                return Ok(student);
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
        [HttpPost("{id}/deactivate")]
        public async Task<IActionResult> DeactivateStudent(Guid id, [FromBody] DeactivateStudentRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var student = await _studentService.DeactivateStudentAsync(id, request.Reason);
                return Ok(student);
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
        [HttpPost("{id}/restore")]
        public async Task<IActionResult> RestoreStudent(Guid id)
        {
            try
            {
                var student = await _studentService.RestoreStudentAsync(id);
                return Ok(student);
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
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStudent(Guid id)
        {
            try
            {
                var result = await _studentService.SoftDeleteStudentAsync(id, "Deleted by admin");
                return Ok(result);
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
        [HttpGet("status/{status}")]
        public async Task<IActionResult> GetStudentsByStatus(int status)
        {
            try
            {
                if (!Enum.IsDefined(typeof(Data.Models.Enums.StudentStatus), status))
                {
                    return BadRequest(new { message = "Invalid status value. Valid values are: 0 (Available), 1 (Pending), 2 (Active), 3 (Inactive), 4 (Deleted)." });
                }

                var studentStatus = (Data.Models.Enums.StudentStatus)status;
                var students = await _studentService.GetStudentsByStatusAsync(studentStatus);
                return Ok(students);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Get all students that have not been assigned to any parent (ParentId is null)
        /// Admin-only endpoint for new parent registration workflow
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpGet("unassigned")]
        public async Task<IActionResult> GetUnassignedStudents()
        {
            try
            {
                var students = await _studentService.GetUnassignedStudentsAsync();
                return Ok(students);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Bulk assign multiple students to a single parent
        /// Admin-only endpoint for new parent registration workflow
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPatch("bulk-assign-parent")]
        public async Task<IActionResult> BulkAssignParent([FromBody] BulkAssignParentRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _studentService.BulkAssignParentAsync(request);
                return Ok(result);
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
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

        /// <summary>
        /// Upload photo for a student - Admin only
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPost("{studentId}/upload-photo")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<ActionResult<object>> UploadStudentPhoto(Guid studentId, IFormFile file)
        {
            try
            {
                if (file == null)
                    return BadRequest(new { message = "No file provided." });

                var fileId = await _studentService.UploadStudentPhotoAsync(studentId, file);
                return Ok(new { FileId = fileId, Message = "Student photo uploaded successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while uploading the photo.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Get student photo file ID
        /// </summary>
        [HttpGet("{studentId}/photo-file-id")]
        public async Task<IActionResult> GetStudentPhotoFileId(Guid studentId)
        {
            try
            {
                var fileId = await _studentService.GetStudentPhotoFileIdAsync(studentId);
                if (!fileId.HasValue)
                    return NotFound(new { message = "Student photo not found." });

                return Ok(new { FileId = fileId.Value });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the photo file ID.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Get student photo - returns the image file directly
        /// </summary>
        [Authorize]
        [HttpGet("{studentId}/photo")]
        public async Task<IActionResult> GetStudentPhoto(Guid studentId)
        {
            try
            {
                var fileId = await _studentService.GetStudentPhotoFileIdAsync(studentId);
                if (!fileId.HasValue)
                    return NotFound(new { message = "Student photo not found." });

                // Get file content directly from FileService
                var fileContent = await _fileService.GetFileAsync(fileId.Value);
                var contentType = await _fileService.GetFileContentTypeAsync(fileId.Value);

                return File(fileContent, contentType);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the photo.", detail = ex.Message });
            }
        }
    }
}
