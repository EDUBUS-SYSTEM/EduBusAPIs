using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Services.Contracts;
using Services.Models.Transaction;
using Data.Models.Enums;
using Constants;
using Utils;

namespace APIs.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        private readonly IStudentService _studentService;
        private readonly ITransportFeeItemService _transportFeeItemService;

        public TransactionController(ITransactionService transactionService, IStudentService studentService, ITransportFeeItemService transportFeeItemService)
        {
            _transactionService = transactionService;
            _studentService = studentService;
            _transportFeeItemService = transportFeeItemService;
        }

        /// <summary>
        /// Create transaction from approved pickup point request
        /// </summary>
        [HttpPost("create-from-pickup-point")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<CreateTransactionFromPickupPointResponse>> CreateFromPickupPoint(
            CreateTransactionFromPickupPointRequest request)
        {
            try
            {
                var result = await _transactionService.CreateTransactionFromPickupPointAsync(request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Get transaction detail by ID
        /// </summary>
        [HttpGet("{transactionId}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Parent}")]
        public async Task<ActionResult<TransactionDetailResponseDto>> GetTransactionDetail(Guid transactionId)
        {
            try
            {
                var result = await _transactionService.GetTransactionDetailAsync(transactionId);
                
                // Security check: Verify parent can access this transaction's data
                if (!AuthorizationHelper.CanAccessParentData(Request.HttpContext, result.ParentId))
                {
                    return Forbid();
                }
                
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Get transaction list with filtering
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<TransactionListResponseDto>> GetTransactionList(
            [FromQuery] TransactionListRequest request)
        {
            try
            {
                var result = await _transactionService.GetTransactionListAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Update transaction status
        /// </summary>
        [HttpPut("{transactionId}/status")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult> UpdateTransactionStatus(
            Guid transactionId, 
            [FromBody] UpdateTransactionStatusRequest request)
        {
            try
            {
                var success = await _transactionService.UpdateTransactionStatusAsync(transactionId, request.Status);
                if (success)
                {
                    return Ok(new { message = "Transaction status updated successfully" });
                }
                return NotFound(new { message = "Transaction not found" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Calculate transport fee with automatic data retrieval
        /// </summary>
        [HttpPost("calculate-fee")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(CalculateFeeResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CalculateFeeResponse>> CalculateFee([FromBody] CalculateFeeRequest request)
        {
            try
            {
                var result = await _transactionService.CalculateTransportFeeAsync(request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Get next semester information
        /// </summary>
        [HttpGet("next-semester")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AcademicSemesterInfo), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<AcademicSemesterInfo>> GetNextSemester()
        {
            try
            {
                var result = await _transactionService.GetNextSemesterAsync();
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete transaction (soft delete)
        /// </summary>
        [HttpDelete("{transactionId}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult> DeleteTransaction(Guid transactionId)
        {
            try
            {
                var success = await _transactionService.DeleteTransactionAsync(transactionId);
                if (success)
                {
                    return Ok(new { message = "Transaction deleted successfully" });
                }
                return NotFound(new { message = "Transaction not found" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Update transaction details
        /// </summary>
        [HttpPut("{transactionId}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult> UpdateTransaction(
            Guid transactionId, 
            [FromBody] UpdateTransactionRequest request)
        {
            try
            {
                var success = await _transactionService.UpdateTransactionAsync(transactionId, request);
                if (success)
                {
                    return Ok(new { message = "Transaction updated successfully" });
                }
                return NotFound(new { message = "Transaction not found" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Get transactions by parent ID
        /// </summary>
        [HttpGet("parent/{parentId}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Parent}")]
        public async Task<ActionResult<TransactionListResponseDto>> GetTransactionsByParent(
            Guid parentId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                // Validate parameters
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                // Security check: Verify parent can access this parent's data
                if (!AuthorizationHelper.CanAccessParentData(Request.HttpContext, parentId))
                {
                    return Forbid();
                }

                var request = new TransactionListRequest
                {
                    ParentId = parentId,
                    Page = page,
                    PageSize = pageSize
                };
                var result = await _transactionService.GetTransactionListAsync(request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Get current parent's transactions (auto-detect parent from JWT token)
        /// </summary>
        [HttpGet("my-transactions")]
        [Authorize(Roles = Roles.Parent)]
        public async Task<ActionResult<TransactionListResponseDto>> GetMyTransactions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                // Validate parameters
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                // Get current parent ID from JWT token
                var currentParentId = AuthorizationHelper.GetCurrentUserId(Request.HttpContext);
                if (!currentParentId.HasValue)
                {
                    return Unauthorized(new { message = "Parent ID not found in token" });
                }

                var request = new TransactionListRequest
                {
                    ParentId = currentParentId.Value,
                    Page = page,
                    PageSize = pageSize
                };
                var result = await _transactionService.GetTransactionListAsync(request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Get transactions by student ID
        /// </summary>
        [HttpGet("student/{studentId}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Parent}")]
        public async Task<ActionResult<TransactionListResponseDto>> GetTransactionsByStudent(
            Guid studentId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                // Validate parameters
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                // Security check: Verify parent can access this student's data
                var student = await _studentService.GetStudentByIdAsync(studentId);
                if (student == null)
                    return NotFound(new { message = "Student not found" });

                if (!AuthorizationHelper.CanAccessStudentData(Request.HttpContext, student.ParentId))
                {
                    return Forbid();
                }

                var result = await _transactionService.GetTransactionsByStudentAsync(studentId, page, pageSize);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Get transaction by transport fee item ID
        /// </summary>
        [HttpGet("by-transport-fee-item/{transportFeeItemId}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Parent}")]
        public async Task<ActionResult<TransactionDetailResponseDto>> GetTransactionByTransportFeeItemId(Guid transportFeeItemId)
        {
            try
            {
                // Security check: Verify parent can access this transport fee item's data
                var transportFeeItem = await _transportFeeItemService.GetDetailAsync(transportFeeItemId);
                if (transportFeeItem == null)
                    return NotFound(new { message = "Transport fee item not found" });

                // Get student to check parent relationship
                var student = await _studentService.GetStudentByIdAsync(transportFeeItem.StudentId);
                if (student == null)
                    return NotFound(new { message = "Student not found" });

                if (!AuthorizationHelper.CanAccessStudentData(Request.HttpContext, student.ParentId))
                {
                    return Forbid();
                }

                var result = await _transactionService.GetTransactionByTransportFeeItemIdAsync(transportFeeItemId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Admin creates a transaction for parent to pay semester fee
        /// For new parent registration workflow
        /// </summary>
        [HttpPost("admin/create")]
        [Authorize(Roles = Roles.Admin)]
        [ProducesResponseType(typeof(AdminCreateTransactionResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<AdminCreateTransactionResponse>> AdminCreateTransaction(
            [FromBody] AdminCreateTransactionRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Get admin ID from claims
            var adminId = AuthorizationHelper.GetCurrentUserId(Request.HttpContext);
            if (!adminId.HasValue)
            {
                return Unauthorized(new { message = "Admin ID not found in token" });
            }

            try
            {
                var result = await _transactionService.AdminCreateTransactionAsync(request, adminId.Value);
                return StatusCode(StatusCodes.Status201Created, result);
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
                return StatusCode(500, new { message = "Internal server error", details = ex.Message });
            }
        }
    }

    public class UpdateTransactionStatusRequest
    {
        public TransactionStatus Status { get; set; }
    }

    public class UpdateTransactionRequest
    {
        public string? Description { get; set; }
        public decimal? Amount { get; set; }
        public string? Currency { get; set; }
    }
}