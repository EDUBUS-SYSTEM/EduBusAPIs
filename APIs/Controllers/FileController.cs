using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Constants;

namespace APIs.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FileController : ControllerBase
    {
        private readonly IFileService _fileService;

        public FileController(IFileService fileService)
        {
            _fileService = fileService;
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<object>> UploadFile(
            IFormFile file,
            [FromForm] string entityType,
            [FromForm] Guid? entityId,
            [FromForm] string fileType)
        {
            try
            {
                if (file == null)
                    return BadRequest("No file provided.");

                if (string.IsNullOrEmpty(entityType) || string.IsNullOrEmpty(fileType))
                    return BadRequest("EntityType and FileType are required.");

                // For template files, use Guid.Empty if entityId is null
                var actualEntityId = entityId ?? Guid.Empty;

                var fileId = await _fileService.UploadFileAsync(actualEntityId, entityType, fileType, file);
                return Ok(new { 
                    FileId = fileId, 
                    Message = "File uploaded successfully.",
                    EntityType = entityType,
                    EntityId = actualEntityId,
                    FileType = fileType
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while uploading the file.");
            }
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpGet("template/{templateType}")]
        public async Task<IActionResult> DownloadTemplate(string templateType)
        {
            try
            {
                var templateFile = await _fileService.GetTemplateFileAsync(templateType);
                if (templateFile == null)
                    return NotFound($"Template file for {templateType} not found.");

                return File(templateFile.FileContent, templateFile.ContentType, templateFile.OriginalFileName);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while downloading the template file.");
            }
        }

        [Authorize(Roles = Roles.AllRoles)]
        [HttpGet("{fileId}")]
        public async Task<IActionResult> DownloadFile(Guid fileId)
        {
            try
            {
                var fileContent = await _fileService.GetFileAsync(fileId);
                var contentType = await _fileService.GetFileContentTypeAsync(fileId);

                return File(fileContent, contentType);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while downloading the file.");
            }
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpDelete("{fileId}")]
        public async Task<ActionResult> DeleteFile(Guid fileId)
        {
            try
            {
                var result = await _fileService.DeleteFileAsync(fileId);
                if (!result)
                    return NotFound("File not found.");

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while deleting the file.");
            }
        }
    }
}
