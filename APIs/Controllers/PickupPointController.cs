using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.PickupPoint;
using System.ComponentModel.DataAnnotations;
using Constants;

namespace APIs.Controllers
{
	/// <summary>
	/// Public (guest/parent) pickup point enrollment flow + Admin moderation.
	/// Frontend provides geolocation (lat/lng) and distance. Backend validates email/otp,
	/// persists requests (Mongo), and allows admin moderation (SQL + Mongo).
	/// </summary>
	[ApiController]
	[Route("api/[controller]")]
	public class PickupPointController : ControllerBase
	{
		private readonly IPickupPointEnrollmentService _svc;

		public PickupPointController(IPickupPointEnrollmentService svc)
		{
			_svc = svc;
		}

		[HttpPost("register")]
		[AllowAnonymous]
		[ProducesResponseType(typeof(ParentRegistrationResponseDto), StatusCodes.Status201Created)]
		[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
		public async Task<IActionResult> RegisterParent([FromBody] ParentRegistrationRequestDto dto)
		{
			if (!ModelState.IsValid) return ValidationProblem(ModelState);

			try
			{
				var result = await _svc.RegisterParentAsync(dto);
				return StatusCode(StatusCodes.Status201Created, result);
			}
			catch (InvalidOperationException ex)
			{
				return Conflict(Problem(title: "Registration conflict", detail: ex.Message,
									statusCode: StatusCodes.Status409Conflict));
			}
			catch (ArgumentException ex)
			{
				return BadRequest(Problem(title: "Invalid registration data", detail: ex.Message,
									  statusCode: StatusCodes.Status400BadRequest));
			}
		}



		[HttpPost("verify-otp")]
		[AllowAnonymous]
		[ProducesResponseType(typeof(VerifyOtpWithStudentsResponseDto), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
		public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest req)
		{
			if (!ModelState.IsValid) return ValidationProblem(ModelState);

			var result = await _svc.VerifyOtpWithStudentsAsync(req.Email, req.Otp);
			return Ok(result);
		}


		[HttpPost("submit-request")]
		[AllowAnonymous]
		[ProducesResponseType(typeof(SubmitPickupPointRequestResponseDto), StatusCodes.Status201Created)]
		[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
		public async Task<IActionResult> SubmitPickupPointRequest([FromBody] SubmitPickupPointRequestDto dto)
		{
			if (!ModelState.IsValid) return ValidationProblem(ModelState);

			try
			{
				var result = await _svc.SubmitPickupPointRequestAsync(dto);
				return StatusCode(StatusCodes.Status201Created, result);
			}
			catch (ArgumentOutOfRangeException ex)
			{
				return BadRequest(Problem(title: "Invalid price range", detail: ex.Message,
									  statusCode: StatusCodes.Status400BadRequest));
			}
			catch (ArgumentException ex)
			{
				return BadRequest(Problem(title: "Invalid request data", detail: ex.Message,
									  statusCode: StatusCodes.Status400BadRequest));
			}
			catch (InvalidOperationException ex)
			{
				return Conflict(Problem(title: "Request conflict", detail: ex.Message,
									statusCode: StatusCodes.Status409Conflict));
			}
		}


		// ===================
		// Admin-only endpoints
		// ===================

		[HttpGet("requests")]
		[Authorize(Roles = Roles.Admin)]
		[ProducesResponseType(typeof(List<PickupPointRequestDetailDto>), StatusCodes.Status200OK)]
		public async Task<IActionResult> ListRequests([FromQuery] PickupPointRequestListQuery query)
		{
			query ??= new PickupPointRequestListQuery();
			if (query.Take <= 0 || query.Take > 200) query.Take = 50;
			if (query.Skip < 0) query.Skip = 0;

			var list = await _svc.ListRequestDetailsAsync(query);
			return Ok(list);
		}

		/// <summary>
		/// Approve a pickup point request.
		/// </summary>
		[HttpPost("requests/{id:guid}/approve")]
		[Authorize(Roles = Roles.Admin)]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
		public async Task<IActionResult> ApproveRequest([FromRoute] Guid id, [FromBody] ApprovePickupPointRequestDto body)
		{
			if (!ModelState.IsValid) return ValidationProblem(ModelState);

			var adminId = ResolveAdminIdFromClaims();
			if (adminId is null || adminId == Guid.Empty)
			{
				return Unauthorized(Problem(title: "Unauthorized",
									  detail: "Admin ID not found in claims.",
									  statusCode: StatusCodes.Status401Unauthorized));
			}

			try
			{
				await _svc.ApproveRequestAsync(id, adminId.Value, body.Notes);
				return NoContent();
			}
			catch (KeyNotFoundException ex)
			{
				return NotFound(Problem(title: "Request not found", detail: ex.Message,
									statusCode: StatusCodes.Status404NotFound));
			}
			catch (InvalidOperationException ex)
			{
				return Conflict(Problem(title: "Cannot approve", detail: ex.Message,
									statusCode: StatusCodes.Status409Conflict));
			}
		}

		/// <summary>
		/// Reject a pickup point request with a reason.
		/// </summary>
		[HttpPost("requests/{id:guid}/reject")]
		[Authorize(Roles = Roles.Admin)]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
		[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
		public async Task<IActionResult> RejectRequest([FromRoute] Guid id, [FromBody] RejectPickupPointRequestDto body)
		{
			if (!ModelState.IsValid) return ValidationProblem(ModelState);
			if (string.IsNullOrWhiteSpace(body.Reason))
				return BadRequest(Problem(title: "Reason required",
									  detail: "Rejection reason must not be empty.",
									  statusCode: StatusCodes.Status400BadRequest));

			var adminId = ResolveAdminIdFromClaims();
			if (adminId is null || adminId == Guid.Empty)
			{
				return Unauthorized(Problem(title: "Unauthorized",
									  detail: "Admin ID not found in claims.",
									  statusCode: StatusCodes.Status401Unauthorized));
			}

			try
			{
				await _svc.RejectRequestAsync(id, adminId.Value, body.Reason.Trim());
				return NoContent();
			}
			catch (KeyNotFoundException ex)
			{
				return NotFound(Problem(title: "Request not found", detail: ex.Message,
									statusCode: StatusCodes.Status404NotFound));
			}
			catch (InvalidOperationException ex)
			{
				return Conflict(Problem(title: "Cannot reject", detail: ex.Message,
									statusCode: StatusCodes.Status409Conflict));
			}
		}

		// ===================
		// Helpers
		// ===================

		private Guid? ResolveAdminIdFromClaims()
		{
			var adminIdStr = User.FindFirst("AdminId")?.Value
						  ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

			return Guid.TryParse(adminIdStr, out var id) ? id : null;
		}
	}
}
