using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.RelocationRequest;
using Utils;

namespace APIs.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize]
	public class RelocationRequestController : ControllerBase
	{
		private readonly IRelocationRequestService _service;

		public RelocationRequestController(IRelocationRequestService service)
		{
			_service = service;
		}

		/// <summary>
		/// Parent: Create a new relocation request
		/// </summary>
		[HttpPost]
		[Authorize(Roles = "Parent")]
		public async Task<IActionResult> CreateRequest([FromBody] CreateRelocationRequestDto dto)
		{
			try
			{
				var parentId = AuthorizationHelper.GetCurrentUserId(HttpContext);
				if (!parentId.HasValue)
					return Unauthorized(new { message = "User ID not found in token." });

				var result = await _service.CreateRequestAsync(dto, parentId.Value);
				return Ok(result);
			}
			catch (KeyNotFoundException ex)
			{
				return NotFound(new { message = ex.Message });
			}
			catch (UnauthorizedAccessException ex)
			{
				return Forbid(ex.Message);
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		/// <summary>
		/// Parent: Get refund calculation preview
		/// </summary>
		[HttpGet("calculate-refund/{studentId}")]
		[Authorize(Roles = "Parent")]
		public async Task<IActionResult> CalculateRefundPreview(
			Guid studentId,
			[FromQuery] double newDistanceKm)
		{
			try
			{
				var result = await _service.CalculateRefundPreviewAsync(studentId, newDistanceKm);
				return Ok(result);
			}
			catch (Exception ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		/// <summary>
		/// Parent: Get my relocation requests
		/// </summary>
		[HttpGet("my-requests")]
		[Authorize(Roles = "Parent")]
		public async Task<IActionResult> GetMyRequests(
			[FromQuery] string? status = null,
			[FromQuery] int page = 1,
			[FromQuery] int perPage = 20)
		{
			var parentId = AuthorizationHelper.GetCurrentUserId(HttpContext);
			if (!parentId.HasValue)
				return Unauthorized(new { message = "User ID not found in token." });

			var result = await _service.GetMyRequestsAsync(parentId.Value, status, page, perPage);
			return Ok(result);
		}

		/// <summary>
		/// Get relocation request by ID
		/// </summary>
		[HttpGet("{requestId}")]
		public async Task<IActionResult> GetRequestById(Guid requestId)
		{
			var result = await _service.GetRequestByIdAsync(requestId);
			if (result == null)
				return NotFound(new { message = "Relocation request not found." });

			return Ok(result);
		}

		/// <summary>
		/// Admin: Get all relocation requests
		/// </summary>
		[HttpGet]
		[Authorize(Roles = "Admin,SuperAdmin")]
		public async Task<IActionResult> GetAllRequests(
			[FromQuery] string? status = null,
			[FromQuery] string? semesterCode = null,
			[FromQuery] DateTime? fromDate = null,
			[FromQuery] DateTime? toDate = null,
			[FromQuery] int page = 1,
			[FromQuery] int perPage = 20)
		{
			var result = await _service.GetAllRequestsAsync(status, semesterCode, fromDate, toDate, page, perPage);
			return Ok(result);
		}

		/// <summary>
		/// Admin: Approve relocation request
		/// </summary>
		[HttpPost("{requestId}/approve")]
		[Authorize(Roles = "Admin,SuperAdmin")]
		public async Task<IActionResult> ApproveRequest(
			Guid requestId,
			[FromBody] ApproveRelocationRequestDto dto)
		{
			try
			{
				var adminId = AuthorizationHelper.GetCurrentUserId(HttpContext);
				if (!adminId.HasValue)
					return Unauthorized(new { message = "User ID not found in token." });

				var result = await _service.ApproveRequestAsync(requestId, dto, adminId.Value);
				return Ok(result);
			}
			catch (KeyNotFoundException ex)
			{
				return NotFound(new { message = ex.Message });
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		/// <summary>
		/// Admin: Reject relocation request
		/// </summary>
		[HttpPost("{requestId}/reject")]
		[Authorize(Roles = "Admin,SuperAdmin")]
		public async Task<IActionResult> RejectRequest(
			Guid requestId,
			[FromBody] RejectRelocationRequestDto dto)
		{
			try
			{
				var adminId = AuthorizationHelper.GetCurrentUserId(HttpContext);
				if (!adminId.HasValue)
					return Unauthorized(new { message = "User ID not found in token." });

				var result = await _service.RejectRequestAsync(requestId, dto, adminId.Value);
				return Ok(result);
			}
			catch (KeyNotFoundException ex)
			{
				return NotFound(new { message = ex.Message });
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}
	}
}