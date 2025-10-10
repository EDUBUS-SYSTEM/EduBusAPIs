using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.TransportFeeItem;
using Data.Models.Enums;
using Constants;
using Utils;

namespace APIs.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransportFeeItemController : ControllerBase
    {
        private readonly ITransportFeeItemService _transportFeeItemService;
        private readonly IStudentService _studentService;
        private readonly ITransactionService _transactionService;

        public TransportFeeItemController(ITransportFeeItemService transportFeeItemService, IStudentService studentService, ITransactionService transactionService)
        {
            _transportFeeItemService = transportFeeItemService;
            _studentService = studentService;
            _transactionService = transactionService;
        }

        /// <summary>
        /// Get transport fee item detail by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Parent}")]
        public async Task<ActionResult<TransportFeeItemDetailResponse>> GetDetail(Guid id)
        {
            try
            {
                var result = await _transportFeeItemService.GetDetailAsync(id);
                
                // Security check: Verify parent can access this transport fee item's data
                var student = await _studentService.GetStudentByIdAsync(result.StudentId);
                if (student == null)
                    return NotFound("Student not found");

                if (!AuthorizationHelper.CanAccessStudentData(Request.HttpContext, student.ParentId))
                {
                    return Forbid();
                }

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get list of transport fee items with filtering and pagination
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<TransportFeeItemListResponse>> GetList([FromQuery] TransportFeeItemListRequest request)
        {
            try
            {
                var result = await _transportFeeItemService.GetListAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Create a new transport fee item
        /// </summary>
        [HttpPost]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<Data.Models.TransportFeeItem>> Create([FromBody] CreateTransportFeeItemRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _transportFeeItemService.CreateAsync(request);
                return CreatedAtAction(nameof(GetDetail), new { id = result.Id }, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Update transport fee item status
        /// </summary>
        [HttpPut("{id}/status")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult> UpdateStatus(Guid id, [FromBody] UpdateTransportFeeItemStatusRequest request)
        {
            try
            {
                if (id != request.Id)
                    return BadRequest("ID mismatch");

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _transportFeeItemService.UpdateStatusAsync(request);
                if (!result)
                    return NotFound("Transport fee item not found");

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Update multiple transport fee items status
        /// </summary>
        [HttpPut("status/batch")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult> UpdateStatusBatch([FromBody] UpdateStatusBatchRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _transportFeeItemService.UpdateStatusBatchAsync(request.Ids, request.Status);
                if (!result)
                    return NotFound("No transport fee items found");

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get transport fee items by transaction ID
        /// </summary>
        [HttpGet("transaction/{transactionId}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Parent}")]
        public async Task<ActionResult<List<TransportFeeItemSummary>>> GetByTransactionId(Guid transactionId)
        {
            try
            {
                // First check if transaction exists
                try
                {
                    await _transactionService.GetTransactionDetailAsync(transactionId);
                }
                catch (KeyNotFoundException)
                {
                    return NotFound("Transaction not found");
                }
                
                var result = await _transportFeeItemService.GetByTransactionIdAsync(transactionId);
                
                // Security check: Verify parent can access these transport fee items
                foreach (var item in result)
                {
                    var student = await _studentService.GetStudentByIdAsync(item.StudentId);
                    if (student != null && !AuthorizationHelper.CanAccessStudentData(Request.HttpContext, student.ParentId))
                    {
                        return Forbid();
                    }
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get transport fee items by student ID
        /// </summary>
        [HttpGet("student/{studentId}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Parent}")]
        public async Task<ActionResult<List<TransportFeeItemSummary>>> GetByStudentId(Guid studentId)
        {
            try
            {
                // Security check: Verify parent can access this student's data
                var student = await _studentService.GetStudentByIdAsync(studentId);
                if (student == null)
                    return NotFound("Student not found");

                if (!AuthorizationHelper.CanAccessStudentData(Request.HttpContext, student.ParentId))
                {
                    return Forbid();
                }

                var result = await _transportFeeItemService.GetByStudentIdAsync(studentId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get transport fee items by parent email (Admin only)
        /// </summary>
        [HttpGet("parent/{parentEmail}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<List<TransportFeeItemSummary>>> GetByParentEmail(string parentEmail)
        {
            try
            {
                // Admin can access any parent's data
                var result = await _transportFeeItemService.GetByParentEmailAsync(parentEmail);
                
                // If no transport fee items found, return empty list
                // (This could mean parent has no transport fee items or email doesn't exist)
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get total amount by transaction ID
        /// </summary>
        [HttpGet("transaction/{transactionId}/total")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Parent}")]
        public async Task<ActionResult<decimal>> GetTotalAmountByTransactionId(Guid transactionId)
        {
            try
            {
                // First check if transaction exists
                try
                {
                    await _transactionService.GetTransactionDetailAsync(transactionId);
                }
                catch (KeyNotFoundException)
                {
                    return NotFound("Transaction not found");
                }

                // Security check: Verify parent can access this transaction's data
                var transportFeeItems = await _transportFeeItemService.GetByTransactionIdAsync(transactionId);

                // Check if parent can access any of the transport fee items
                foreach (var item in transportFeeItems)
                {
                    var student = await _studentService.GetStudentByIdAsync(item.StudentId);
                    if (student != null && !AuthorizationHelper.CanAccessStudentData(Request.HttpContext, student.ParentId))
                    {
                        return Forbid();
                    }
                }

                var result = await _transportFeeItemService.GetTotalAmountByTransactionIdAsync(transactionId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get count by status
        /// </summary>
        [HttpGet("count/{status}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<int>> GetCountByStatus(TransportFeeItemStatus status)
        {
            try
            {
                var result = await _transportFeeItemService.GetCountByStatusAsync(status);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Delete transport fee item (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult> Delete(Guid id)
        {
            try
            {
                var result = await _transportFeeItemService.DeleteAsync(id);
                if (!result)
                    return NotFound("Transport fee item not found");

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }

    public class UpdateStatusBatchRequest
    {
        public List<Guid> Ids { get; set; } = new();
        public TransportFeeItemStatus Status { get; set; }
    }
}
