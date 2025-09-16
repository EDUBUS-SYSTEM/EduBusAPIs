using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Services.Models.Payment;
using Services.Utils;
using Microsoft.Extensions.Options;

namespace APIs.Controllers;

/// <summary>
/// Test controller for PayOS signature validation
/// This controller should be removed in production
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")] // Only admins can access test endpoints
public class PayOSTestController : ControllerBase
{
    private readonly IPayOSService _payOSService;
    private readonly ILogger<PayOSTestController> _logger;
    private readonly PayOSConfig _config;

    public PayOSTestController(
        IPayOSService payOSService, 
        ILogger<PayOSTestController> logger,
        IOptions<PayOSConfig> config)
    {
        _payOSService = payOSService;
        _logger = logger;
        _config = config.Value;
    }

    /// <summary>
    /// Test PayOS signature validation with sample data
    /// </summary>
    [HttpPost("test-signature")]
    public async Task<ActionResult<PayOSSignatureTestResult>> TestSignatureValidation()
    {
        try
        {
            var result = await PayOSSignatureHelper.TestSignatureValidationAsync(_config.ChecksumKey);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing PayOS signature validation");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Test signature generation with custom data
    /// </summary>
    [HttpPost("test-signature-custom")]
    public async Task<ActionResult<object>> TestSignatureGeneration([FromBody] PayOSWebhookData data)
    {
        try
        {
            var signature = await _payOSService.GenerateSignatureAsync(data);
            
            return Ok(new
            {
                data = data,
                generatedSignature = signature,
                checksumKey = _config.ChecksumKey.Substring(0, 8) + "..." // Show only first 8 chars for security
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing PayOS signature generation");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Test signature verification with custom data and signature
    /// </summary>
    [HttpPost("test-signature-verify")]
    public async Task<ActionResult<object>> TestSignatureVerification([FromBody] PayOSSignatureTestRequest request)
    {
        try
        {
            var isValid = await _payOSService.VerifyPayOSWebhookSignatureAsync(request.Data, request.Signature);
            
            return Ok(new
            {
                data = request.Data,
                signature = request.Signature,
                isValid = isValid,
                message = isValid ? "Signature is valid" : "Signature is invalid"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing PayOS signature verification");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Get PayOS configuration (without sensitive data)
    /// </summary>
    [HttpGet("config")]
    public ActionResult<object> GetConfig()
    {
        return Ok(new
        {
            baseUrl = _config.BaseUrl,
            clientId = _config.ClientId.Substring(0, 8) + "...", // Show only first 8 chars
            checksumKey = _config.ChecksumKey.Substring(0, 8) + "...", // Show only first 8 chars
            webhookUrl = _config.WebhookUrl,
            returnUrl = _config.ReturnUrl,
            cancelUrl = _config.CancelUrl,
            qrExpirationMinutes = _config.QrExpirationMinutes
        });
    }
}

/// <summary>
/// Request model for signature verification test
/// </summary>
public class PayOSSignatureTestRequest
{
    public PayOSWebhookData Data { get; set; } = new();
    public string Signature { get; set; } = string.Empty;
}

