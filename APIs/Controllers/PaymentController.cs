using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.Payment;
using System.Security.Claims;
using Constants;
using Data.Models.Enums;

namespace APIs.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PagedTransactionResponse>> GetTransactions(
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

            var result = await _paymentService.GetTransactionsAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transactions");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    // Removed simple create-and-QR endpoint per user's request

    [HttpGet("unpaid-fees")]
    [Authorize(Roles = Roles.Parent)]
    public async Task<ActionResult<UnpaidFeesResponse>> GetUnpaidFees()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var parentId))
            {
                return Unauthorized(new { message = "Invalid user identity" });
            }

            var result = await _paymentService.GetUnpaidFeesAsync(parentId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unpaid fees for parent");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TransactionDetailResponse>> GetTransactionDetail(Guid id)
    {
        try
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var transaction = await _paymentService.GetTransactionDetailAsync(id);
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

    [HttpPost("{id}/qrcode")]
    public async Task<ActionResult<QrResponse>> GenerateOrRefreshQr(Guid id)
    {
        try
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Validate parent access
            if (userRole == Roles.Parent)
            {
                var transaction = await _paymentService.GetTransactionDetailAsync(id);
                if (transaction == null)
                    return NotFound(new { message = "Transaction not found" });

                if (transaction.ParentId != Guid.Parse(userId!))
                    return Forbid("Parents can only access their own transactions");
            }

            var result = await _paymentService.GenerateOrRefreshQrAsync(id);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating/refreshing QR for transaction: {TransactionId}", id);
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    [HttpGet("{id}/events")]
    public async Task<ActionResult<IEnumerable<PaymentEventResponse>>> GetTransactionEvents(Guid id)
    {
        try
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Validate parent access
            if (userRole == Roles.Parent)
            {
                var transaction = await _paymentService.GetTransactionDetailAsync(id);
                if (transaction == null)
                    return NotFound(new { message = "Transaction not found" });

                if (transaction.ParentId != Guid.Parse(userId!))
                    return Forbid("Parents can only access their own transactions");
            }

            var events = await _paymentService.GetTransactionEventsAsync(id);
            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction events for ID: {TransactionId}", id);
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    [HttpPost("{id}/cancel")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<TransactionSummaryResponse>> CancelTransaction(Guid id)
    {
        try
        {
            var result = await _paymentService.CancelTransactionAsync(id);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling transaction: {TransactionId}", id);
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    [HttpPost("{id}/mark-paid")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<TransactionSummaryResponse>> MarkTransactionAsPaid(
        Guid id, 
        [FromBody] MarkPaidRequest? request = null)
    {
        try
        {
            var result = await _paymentService.MarkTransactionAsPaidAsync(id, request ?? new MarkPaidRequest());
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking transaction as paid: {TransactionId}", id);
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// PayOS webhook
    /// Receives payment status updates from PayOS.
    /// Validates signature and updates transaction status accordingly.
    /// Idempotent - duplicate webhooks are ignored based on orderCode.
    /// </summary>
    [HttpPost("webhook/payos")]
    [AllowAnonymous]
    public async Task<IActionResult> HandlePayOSWebhook([FromBody] PayOSWebhookPayload payload)
    {
        try
        {
            if(payload.Data.OrderCode == 123) return Ok(); // Test webhook endpoint
            _logger.LogInformation("PayOS webhook received: {Payload}", payload);
            
            // Process webhook and update transaction status
            var success = await _paymentService.HandlePayOSWebhookAsync(payload);

            if (success)
                return Ok(new { message = "Webhook acknowledged and processed" });
            else
                return BadRequest(new { message = "Invalid signature or malformed payload" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling PayOS webhook");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Handle PayOS payment return URL
    /// This endpoint is called when user completes payment successfully
    /// </summary>
    [HttpGet("return")]
    [AllowAnonymous]
    public async Task<IActionResult> HandlePaymentReturn(
        [FromQuery] string code,
        [FromQuery] string id,
        [FromQuery] bool cancel,
        [FromQuery] string status,
        [FromQuery] long orderCode)
    {
        try
        {
            _logger.LogInformation("Payment return received: Code={Code}, Id={Id}, Cancel={Cancel}, Status={Status}, OrderCode={OrderCode}", 
                code, id, cancel, status, orderCode);

            // Find transaction by order code
            var transaction = await _paymentService.GetTransactionByOrderCodeAsync(orderCode);
            
            if (transaction == null)
            {
                return BadRequest(new { message = "Transaction not found" });
            }

            // Determine payment status
            var paymentStatus = cancel ? "cancelled" : 
                              (code == "00" && status == "PAID") ? "success" : "failed";

            // Return appropriate response based on status
            return paymentStatus switch
            {
                "success" => Ok(new 
                { 
                    message = "Payment completed successfully",
                    transactionId = transaction.Id,
                    orderCode = orderCode,
                    status = "paid"
                }),
                "cancelled" => Ok(new 
                { 
                    message = "Payment was cancelled",
                    transactionId = transaction.Id,
                    orderCode = orderCode,
                    status = "cancelled"
                }),
                _ => Ok(new 
                { 
                    message = "Payment failed",
                    transactionId = transaction.Id,
                    orderCode = orderCode,
                    status = "failed"
                })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling payment return");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Handle PayOS payment cancel URL
    /// This endpoint is called when user cancels payment
    /// </summary>
    [HttpGet("cancel")]
    [AllowAnonymous]
    public async Task<IActionResult> HandlePaymentCancel(
        [FromQuery] string code,
        [FromQuery] string id,
        [FromQuery] bool cancel,
        [FromQuery] string status,
        [FromQuery] long orderCode)
    {
        try
        {
            _logger.LogInformation("Payment cancel received: Code={Code}, Id={Id}, Cancel={Cancel}, Status={Status}, OrderCode={OrderCode}", 
                code, id, cancel, status, orderCode);

            // Find transaction by order code
            var transaction = await _paymentService.GetTransactionByOrderCodeAsync(orderCode);
            
            if (transaction == null)
            {
                return BadRequest(new { message = "Transaction not found" });
            }

            return Ok(new 
            { 
                message = "Payment was cancelled by user",
                transactionId = transaction.Id,
                orderCode = orderCode,
                status = "cancelled"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling payment cancel");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }
}
