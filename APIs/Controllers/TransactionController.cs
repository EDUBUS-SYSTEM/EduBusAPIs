using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Services.Contracts;
using Services.Models.Transaction;
using Data.Models.Enums;

namespace APIs.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        /// <summary>
        /// Create transaction from approved pickup point request
        /// </summary>
        [HttpPost("create-from-pickup-point")]
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
        public async Task<ActionResult<TransactionDetailResponseDto>> GetTransactionDetail(Guid transactionId)
        {
            try
            {
                var result = await _transactionService.GetTransactionDetailAsync(transactionId);
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
        /// Get transactions by student ID
        /// </summary>
        [HttpGet("student/{studentId}")]
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
        public async Task<ActionResult<TransactionDetailResponseDto>> GetTransactionByTransportFeeItemId(Guid transportFeeItemId)
        {
            try
            {
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