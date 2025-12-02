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
		private readonly IPickupPointService _pickupPointService;

		public PickupPointController(IPickupPointEnrollmentService svc, IPickupPointService pickupPointService)
		{
			_svc = svc;
			_pickupPointService = pickupPointService;
		}

		// ===================================================================================
		// DEPRECATED ENDPOINTS - Old Public Parent Registration Flow
		// These endpoints are deprecated in favor of admin-managed parent registration
		// New workflow: Admin creates parent account via /api/Student/bulk-assign-parent,
		//               /api/PickupPoint/admin/create, and /api/Transaction/admin/create
		// ===================================================================================

		/// <summary>
		/// [DEPRECATED] Public parent registration endpoint
		/// Use admin-managed workflow instead
		/// </summary>
		[Obsolete("This endpoint is deprecated. Use admin-managed parent registration workflow instead.")]
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

		[HttpGet("registration/eligibility")]
		[Authorize(Roles = Roles.Parent)]
		[ProducesResponseType(typeof(ParentRegistrationEligibilityDto), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
		public async Task<IActionResult> GetRegistrationEligibility()
		{
			var parentId = ResolveParentIdFromClaims();
			if (parentId is null)
			{
				return Unauthorized(Problem(title: "Unauthorized",
									  detail: "Parent ID not found in claims.",
									  statusCode: StatusCodes.Status401Unauthorized));
			}

			var parentEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
				?? User.FindFirst("email")?.Value
				?? User.FindFirst("Email")?.Value;

			var result = await _svc.GetRegistrationEligibilityAsync(parentId.Value, parentEmail);
			return Ok(result);
		}



		/// <summary>
		/// [DEPRECATED] OTP verification endpoint for public parent registration
		/// Use admin-managed workflow instead
		/// </summary>
		[Obsolete("This endpoint is deprecated. Use admin-managed parent registration workflow instead.")]
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


		/// <summary>
		/// [DEPRECATED] Public pickup point request submission
		/// Use admin-managed workflow instead
		/// </summary>
		[Obsolete("This endpoint is deprecated. Use admin-managed parent registration workflow instead.")]
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

		[HttpGet("unassigned")]
		[Authorize(Roles = Roles.Admin)]
		[ProducesResponseType(typeof(PickupPointsResponse), StatusCodes.Status200OK)]
		public async Task<IActionResult> GetUnassignedPickupPoints()
		{
			try
			{
				var result = await _pickupPointService.GetUnassignedPickupPointsAsync();
				return Ok(result);
			}
			catch (Exception ex)
			{
				return StatusCode(StatusCodes.Status500InternalServerError,
					new { message = "Internal server error", detail = ex.Message });
			}
		}

		/// <summary>
		/// Get all pickup points with their assigned student status
		/// </summary>
		[HttpGet("with-student-status")]
		[Authorize(Roles = Roles.Admin)]
		[ProducesResponseType(typeof(List<PickupPointWithStudentStatusDto>), StatusCodes.Status200OK)]
		public async Task<IActionResult> GetPickupPointsWithStudentStatus()
		{
			try
			{
				var result = await _svc.GetPickupPointsWithStudentStatusAsync();
				return Ok(result);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Internal server error", details = ex.Message });
			}
		}

		/// <summary>
		/// Admin creates a pickup point directly (auto-approved, no approval workflow)
		/// For new parent registration workflow
		/// </summary>
		[HttpPost("admin/create")]
		[Authorize(Roles = Roles.Admin)]
		[ProducesResponseType(typeof(AdminCreatePickupPointResponse), StatusCodes.Status201Created)]
		[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
		public async Task<IActionResult> AdminCreatePickupPoint([FromBody] AdminCreatePickupPointRequest request)
		{
			if (!ModelState.IsValid)
				return ValidationProblem(ModelState);

			var adminId = ResolveAdminIdFromClaims();
			if (adminId is null || adminId == Guid.Empty)
			{
				return Unauthorized(Problem(title: "Unauthorized",
									  detail: "Admin ID not found in claims.",
									  statusCode: StatusCodes.Status401Unauthorized));
			}

			try
			{
				var result = await _pickupPointService.AdminCreatePickupPointAsync(request, adminId.Value);
				return StatusCode(StatusCodes.Status201Created, result);
			}
			catch (ArgumentNullException ex)
			{
				return BadRequest(Problem(title: "Invalid request", detail: ex.Message,
									  statusCode: StatusCodes.Status400BadRequest));
			}
			catch (KeyNotFoundException ex)
			{
				return NotFound(Problem(title: "Resource not found", detail: ex.Message,
									statusCode: StatusCodes.Status404NotFound));
			}
			catch (InvalidOperationException ex)
			{
				return Conflict(Problem(title: "Operation conflict", detail: ex.Message,
									statusCode: StatusCodes.Status409Conflict));
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Internal server error", details = ex.Message });
			}
		}

		/// <summary>
		/// Reset pickup points by semester
		/// Admin selects semester (e.g., S1 2025-2026) with start and end dates
		/// System finds all StudentPickupPoint records matching the semester criteria
		/// and assigns PickupPointId to CurrentPickupPointId in Student table
		/// </summary>
		[HttpPost("admin/reset-by-semester")]
		[Authorize(Roles = Roles.Admin)]
		[ProducesResponseType(typeof(ResetPickupPointBySemesterResponse), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
		public async Task<IActionResult> ResetPickupPointBySemester([FromBody] ResetPickupPointBySemesterRequest request)
		{
			if (!ModelState.IsValid)
				return ValidationProblem(ModelState);

			var adminId = ResolveAdminIdFromClaims();
			if (adminId is null || adminId == Guid.Empty)
			{
				return Unauthorized(Problem(title: "Unauthorized",
									  detail: "Admin ID not found in claims.",
									  statusCode: StatusCodes.Status401Unauthorized));
			}

			try
			{
				var result = await _pickupPointService.ResetPickupPointBySemesterAsync(request, adminId.Value);
				return Ok(result);
			}
			catch (ArgumentNullException ex)
			{
				return BadRequest(Problem(title: "Invalid request", detail: ex.Message,
									  statusCode: StatusCodes.Status400BadRequest));
			}
			catch (ArgumentException ex)
			{
				return BadRequest(Problem(title: "Invalid date range", detail: ex.Message,
									  statusCode: StatusCodes.Status400BadRequest));
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Internal server error", details = ex.Message });
			}
		}

		/// <summary>
		/// Get all pickup points with their assigned students by semester
		/// Returns pickup points from StudentPickupPoint table filtered by semester criteria
		/// Used for displaying pickup points before reset operation
		/// </summary>
		[HttpPost("admin/get-by-semester")]
		[Authorize(Roles = Roles.Admin)]
		[ProducesResponseType(typeof(GetPickupPointsBySemesterResponse), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
		public async Task<IActionResult> GetPickupPointsBySemester([FromBody] GetPickupPointsBySemesterRequest request)
		{
			if (!ModelState.IsValid)
				return ValidationProblem(ModelState);

			try
			{
				var result = await _pickupPointService.GetPickupPointsBySemesterAsync(request);
				return Ok(result);
			}
			catch (ArgumentNullException ex)
			{
				return BadRequest(Problem(title: "Invalid request", detail: ex.Message,
									  statusCode: StatusCodes.Status400BadRequest));
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Internal server error", details = ex.Message });
			}
		}

		/// <summary>
		/// Get all available semesters from StudentPickupPoint table
		/// Returns distinct semesters that have pickup point assignments
		/// Used for populating semester dropdown in reset form
		/// </summary>
		[HttpGet("admin/available-semesters")]
		[Authorize(Roles = Roles.Admin)]
		[ProducesResponseType(typeof(GetAvailableSemestersResponse), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
		public async Task<IActionResult> GetAvailableSemesters()
		{
			try
			{
				var result = await _pickupPointService.GetAvailableSemestersAsync();
				return Ok(result);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Internal server error", details = ex.Message });
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

		private Guid? ResolveParentIdFromClaims()
		{
			var parentIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
			return Guid.TryParse(parentIdStr, out var id) ? id : null;
		}
	}
}
