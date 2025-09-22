using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.Transaction;
using System.Security.Claims;
using Constants;
using Data.Models.Enums;

namespace APIs.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly ILogger<TransactionController> _logger;

    public TransactionController(ITransactionService transactionService, ILogger<TransactionController> logger)
    {
        _transactionService = transactionService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PagedTransactionDto>> GetTransactions(
        [FromQuery] TransactionStatus? status,
        [FromQuery] Guid? parentId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 20,
        [FromQuery] string sortBy = "createdAtUtc",
        [FromQuery] string sortOrder = "desc")
    {
        try
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var request = new TransactionListRequest
            {
                Status = status,
                ParentId = userRole == Roles.Parent ? Guid.Parse(userId!) : parentId,
                From = from,
                To = to,
                Page = page,
                PerPage = perPage,
                SortBy = sortBy,
                SortOrder = sortOrder
            };

            // Validate parent access
            if (userRole == Roles.Parent && parentId.HasValue && parentId.Value != Guid.Parse(userId!))
            {
                return Forbid("Parents can only access their own transactions");
            }

            var result = await _transactionService.GetTransactionsAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transactions");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TransactionDetailDto>> GetTransaction(Guid id)
    {
        try
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var transaction = await _transactionService.GetTransactionDetailAsync(id);
            if (transaction == null)
                return NotFound(new { message = "Transaction not found" });

            // Validate parent access
            if (userRole == Roles.Parent && transaction.ParentId != Guid.Parse(userId!))
            {
                return Forbid("Parents can only access their own transactions");
            }

            return Ok(transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction detail for ID: {TransactionId}", id);
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<TransactionDetailDto>> CreateTransaction([FromBody] CreateTransactionRequestDto request)
    {
        try
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Set parent ID for parent users
            if (userRole == Roles.Parent)
            {
                request.ParentId = Guid.Parse(userId!);
            }

            var result = await _transactionService.CreateTransactionAsync(request);
            return CreatedAtAction(nameof(GetTransaction), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating transaction");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    [HttpPost("{id}/approve")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<TransactionDetailDto>> ApproveTransaction(Guid id)
    {
        try
        {
            var result = await _transactionService.ApproveTransactionAsync(id);
            if (result == null)
                return NotFound(new { message = "Transaction not found" });

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving transaction: {TransactionId}", id);
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    [HttpPost("{id}/reject")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<TransactionDetailDto>> RejectTransaction(
        Guid id,
        [FromBody] RejectTransactionRequestDto request)
    {
        try
        {
            var result = await _transactionService.RejectTransactionAsync(id, request);
            if (result == null)
                return NotFound(new { message = "Transaction not found" });

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting transaction: {TransactionId}", id);
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    [HttpGet("{id}/history")]
    public async Task<ActionResult<List<TransactionHistoryDto>>> GetTransactionHistory(Guid id)
    {
        try
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Validate parent access
            if (userRole == Roles.Parent)
            {
                var transaction = await _transactionService.GetTransactionDetailAsync(id);
                if (transaction == null)
                    return NotFound(new { message = "Transaction not found" });

                if (transaction.ParentId != Guid.Parse(userId!))
                    return Forbid("Parents can only access their own transactions");
            }

            var result = await _transactionService.GetTransactionHistoryAsync(id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction history for ID: {TransactionId}", id);
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    [HttpGet("parent/{parentId}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<List<TransactionSummaryDto>>> GetParentTransactions(Guid parentId)
    {
        try
        {
            var result = await _transactionService.GetParentTransactionsAsync(parentId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting parent transactions for ID: {ParentId}", parentId);
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }
}
