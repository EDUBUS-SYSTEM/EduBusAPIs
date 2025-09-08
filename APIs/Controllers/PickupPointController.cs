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

		// ===========================
		// Public (guest/parent) APIs
		// ===========================

		/// <summary>
		/// Check if the input email exists in the system (linked to at least one student).
		/// </summary>
		[HttpPost("check-email")]
		[AllowAnonymous]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
		public async Task<IActionResult> CheckEmail([FromBody] CheckEmailRequest req)
		{
			if (!ModelState.IsValid) return ValidationProblem(ModelState);

			var exists = await _svc.CheckParentEmailExistsAsync(req.Email);
			// Trả về object đơn giản để tránh phải tạo thêm DTO Response
			return Ok(new { email = req.Email, exists });
		}

		/// <summary>
		/// Send OTP to parent email if the email is valid in the system.
		/// </summary>
		[HttpPost("send-otp")]
		[AllowAnonymous]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
		[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
		public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest req)
		{
			if (!ModelState.IsValid) return ValidationProblem(ModelState);

			try
			{
				await _svc.SendOtpAsync(req.Email);
				return NoContent();
			}
			catch (InvalidOperationException ex)
			{
				var normalized = ex.Message.ToLowerInvariant();

				if (normalized.Contains("rate") || normalized.Contains("too many"))
				{
					return StatusCode(StatusCodes.Status429TooManyRequests,
						Problem(title: "Rate limit exceeded", detail: ex.Message,
								statusCode: StatusCodes.Status429TooManyRequests));
				}

				if (normalized.Contains("otp"))
				{
					return Conflict(Problem(title: "OTP conflict", detail: ex.Message,
											statusCode: StatusCodes.Status409Conflict));
				}

				return BadRequest(Problem(title: "Cannot send OTP", detail: ex.Message,
										  statusCode: StatusCodes.Status400BadRequest));
			}
		}

		/// <summary>
		/// Verify OTP that was sent to the given email.
		/// </summary>
		[HttpPost("verify-otp")]
		[AllowAnonymous]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
		public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest req)
		{
			if (!ModelState.IsValid) return ValidationProblem(ModelState);

			var ok = await _svc.VerifyOtpAsync(req.Email, req.Otp);
			return Ok(new
			{
				email = req.Email,
				verified = ok,
				message = ok ? "OTP verified successfully." : "Invalid or expired OTP."
			});
		}

		/// <summary>
		/// Get students linked to the provided parent email (for the registration form).
		/// </summary>
		[HttpGet("students")]
		[AllowAnonymous]
		[ProducesResponseType(typeof(List<StudentBriefDto>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
		public async Task<IActionResult> GetStudents([FromQuery][Required, EmailAddress] string email)
		{
			if (!ModelState.IsValid) return ValidationProblem(ModelState);

			var exists = await _svc.CheckParentEmailExistsAsync(email);
			if (!exists)
				return BadRequest(Problem(title: "Invalid email",
										  detail: "Email does not exist in the system.",
										  statusCode: StatusCodes.Status400BadRequest));

			var list = await _svc.GetStudentsByEmailAsync(email);
			return Ok(list);
		}

		/// <summary>
		/// Create a pickup point request (stored in Mongo) after the parent finishes the form.
		/// NOTE: Frontend sends AddressText + Location{Lat,Lng,PlaceId?} + DistanceKm already computed.
		/// </summary>
		[HttpPost("requests")]
		[AllowAnonymous]
		[ProducesResponseType(StatusCodes.Status201Created)]
		[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
		public async Task<IActionResult> CreateRequest([FromBody] CreatePickupPointRequestDto dto)
		{
			if (!ModelState.IsValid) return ValidationProblem(ModelState);

			try
			{
				var doc = await _svc.CreateRequestAsync(dto);
				// Không dùng CreatedAtAction để khỏi cần GET /requests/{id} khi service chưa có
				return StatusCode(StatusCodes.Status201Created, doc);
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

		/// <summary>
		/// List pickup point requests with optional filters (status/email) and paging.
		/// </summary>
		[HttpGet("requests")]
		[Authorize(Roles = Roles.Admin)]
		[ProducesResponseType(typeof(List<PickupPointRequestDocument>), StatusCodes.Status200OK)]
		public async Task<IActionResult> ListRequests([FromQuery] PickupPointRequestListQuery query)
		{
			query ??= new PickupPointRequestListQuery();
			if (query.Take <= 0 || query.Take > 200) query.Take = 50;
			if (query.Skip < 0) query.Skip = 0;

			var list = await _svc.ListRequestsAsync(query);
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
