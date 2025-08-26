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
        [HttpPost("user-photo/{userId}")]
        public async Task<ActionResult<object>> UploadUserPhoto(Guid userId, IFormFile file)
        {
            try
            {
                if (file == null)
                    return BadRequest("No file provided.");

                var fileId = await _fileService.UploadUserPhotoAsync(userId, file);
                return Ok(new { FileId = fileId, Message = "User photo uploaded successfully." });
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

        [Authorize(Roles = $"{Roles.Admin},{Roles.Driver}")]
        [HttpPost("health-certificate/{driverId}")]
        public async Task<ActionResult<object>> UploadHealthCertificate(Guid driverId, IFormFile file)
        {
            try
            {
                if (file == null)
                    return BadRequest("No file provided.");

                var fileId = await _fileService.UploadHealthCertificateAsync(driverId, file);
                return Ok(new { FileId = fileId, Message = "Health certificate uploaded successfully." });
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

        [Authorize(Roles = $"{Roles.Admin},{Roles.Driver}")]
        [HttpPost("license-image/{driverLicenseId}")]
        public async Task<ActionResult<object>> UploadLicenseImage(Guid driverLicenseId, IFormFile file)
        {
            try
            {
                if (file == null)
                    return BadRequest("No file provided.");

                var fileId = await _fileService.UploadLicenseImageAsync(driverLicenseId, file);
                return Ok(new { FileId = fileId, Message = "License image uploaded successfully." });
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
