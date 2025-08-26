using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Data.Models;
using Constants;
using Microsoft.AspNetCore.Authorization;
using Services.Models.UserAccount;
using Utils;

namespace APIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserAccountController : ControllerBase
    {
        private readonly IFileService _fileService;
        private readonly IUserService _userService;
        
        public UserAccountController(IFileService fileService, IUserService userService)
        {
            _fileService = fileService;
            _userService = userService;
        }

        /// <summary>
        /// Get list of users with pagination, filtering, and sorting
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpGet]
        public async Task<ActionResult<UserListResponse>> GetUsers(
            [FromQuery] string? status,
            [FromQuery] string? sortBy,
            [FromQuery] string? sortOrder,
            [FromQuery] int page = 1,
            [FromQuery] int perPage = 20)
        {
            try
            {
                if (page < 1) page = 1;
                if (perPage < 1 || perPage > 100) perPage = 20;

                var result = await _userService.GetUsersAsync(status, page, perPage, sortBy, sortOrder);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "An error occurred while retrieving users",
                    Details = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Get user by ID - Users can view their own info, Admin can view any user
        /// </summary>
        [HttpGet("{userId}")]
        public async Task<ActionResult<UserResponse>> GetUserById(Guid userId)
        {
            try
            {
                if (!AuthorizationHelper.CanAccessUserData(Request.HttpContext, userId))
                {
                    return Forbid();
                }

                var user = await _userService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new ErrorResponse
                    {
                        Message = "User not found",
                        StatusCode = 404
                    });
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "An error occurred while retrieving the user",
                    Details = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Update a user (full update) - Users can update their own info, Admin can update any user
        /// </summary>
        [HttpPut("{userId}")]
        public async Task<ActionResult<UserResponse>> UpdateUser(Guid userId, [FromBody] UserUpdateRequest request)
        {
            try
            {
                if (!AuthorizationHelper.CanAccessUserData(Request.HttpContext, userId))
                {
                    return Forbid();
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Invalid request data",
                        Details = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)),
                        StatusCode = 400
                    });
                }

                var updatedUser = await _userService.UpdateUserAsync(userId, request);
                return Ok(updatedUser);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = ex.Message,
                    StatusCode = 400
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "An error occurred while updating the user",
                    Details = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Update a user (partial update) - Users can update their own info, Admin can update any user
        /// </summary>
        [HttpPatch("{userId}")]
        public async Task<ActionResult<UserResponse>> PartialUpdateUser(Guid userId, [FromBody] UserPartialUpdateRequest request)
        {
            try
            {
                if (!AuthorizationHelper.CanAccessUserData(Request.HttpContext, userId))
                {
                    return Forbid();
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Invalid request data",
                        Details = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)),
                        StatusCode = 400
                    });
                }

                var updatedUser = await _userService.PartialUpdateUserAsync(userId, request);
                return Ok(updatedUser);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = ex.Message,
                    StatusCode = 400
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "An error occurred while updating the user",
                    Details = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Delete a user - Only Admin can delete users
        /// </summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpDelete("{userId}")]
        public async Task<ActionResult<BasicSuccessResponse>> DeleteUser(Guid userId)
        {
            try
            {
                var result = await _userService.DeleteUserAsync(userId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = ex.Message,
                    StatusCode = 400
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Message = "An error occurred while deleting the user",
                    Details = ex.Message,
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Upload user photo - Users can upload their own photo, Admin can upload for any user
        /// </summary>
        [HttpPost("{userId}/upload-user-photo")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<object>> UploadUserPhoto(Guid userId, IFormFile file)
        {
            try
            {
                if (!AuthorizationHelper.CanAccessUserData(Request.HttpContext, userId))
                {
                    return Forbid();
                }

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
                Console.WriteLine($"Error uploading user photo: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return StatusCode(500, $"An error occurred while uploading the user photo: {ex.Message}");
            }
        }

        /// <summary>
        /// Get user photo - Users can view their own photo, Admin can view any user's photo
        /// </summary>
        [HttpGet("{userId}/user-photo")]
        public async Task<IActionResult> GetUserPhoto(Guid userId)
        {
            try
            {
                if (!AuthorizationHelper.CanAccessUserData(Request.HttpContext, userId))
                {
                    return Forbid();
                }

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
