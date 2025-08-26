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
