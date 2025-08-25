using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Data.Models;
using Constants;
using Microsoft.AspNetCore.Authorization;

namespace APIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserAccountController : ControllerBase
    {
        private readonly IFileService _fileService;
        
        public UserAccountController(IFileService fileService)
        {
            _fileService = fileService;
        }

        [Authorize(Roles = Roles.AllRoles)]
        [HttpPost("{userId}/upload-user-photo")]
        [Consumes("multipart/form-data")]
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
                // Log the actual exception for debugging
                Console.WriteLine($"Error uploading user photo: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return StatusCode(500, $"An error occurred while uploading the user photo: {ex.Message}");
            }
        }

        [Authorize(Roles = Roles.AllRoles)]
        [HttpGet("{userId}/user-photo")]
        public async Task<IActionResult> GetUserPhoto(Guid userId)
        {
            try
            {
                var fileId = await _fileService.GetUserPhotoFileIdAsync(userId);
                if (!fileId.HasValue)
                    return NotFound("User photo not found.");

                var fileContent = await _fileService.GetFileAsync(fileId.Value);
                var contentType = await _fileService.GetFileContentTypeAsync(fileId.Value);

                return File(fileContent, contentType);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while retrieving the user photo.");
            }
        }
    }
}
