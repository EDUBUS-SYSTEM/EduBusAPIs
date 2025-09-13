using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.Contracts;
using Services.Models.Payment;
using System.Text.Json;

namespace Services.Implementations;

public class PayOSService : IPayOSService
{
    private readonly PayOSConfig _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger<PayOSService> _logger;

    public PayOSService(
        IOptions<PayOSConfig> config,
        HttpClient httpClient,
        ILogger<PayOSService> logger)
    {
        _config = config.Value;
        _httpClient = httpClient;
        _logger = logger;
        
        // Configure HttpClient
        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("x-client-id", _config.ClientId);
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _config.ApiKey);
    }

    public async Task<PayOSCreatePaymentResponse> CreatePaymentAsync(PayOSCreatePaymentRequest request)
    {
        try
        {
            var payload = new
            {
                orderCode = request.OrderCode,
                amount = request.Amount,
                description = request.Description,
                items = request.Items,
                returnUrl = request.ReturnUrl,
                cancelUrl = request.CancelUrl
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/v2/payment-requests", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("PayOS API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                throw new HttpRequestException($"PayOS API error: {response.StatusCode}");
            }

            var result = JsonSerializer.Deserialize<PayOSCreatePaymentResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null)
            {
                throw new InvalidOperationException("Failed to deserialize PayOS response");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PayOS payment for order: {OrderCode}", request.OrderCode);
            throw;
        }
    }

    public async Task<PayOSPaymentResponse> GetPaymentInfoAsync(string orderCode)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/v2/payment-requests/{orderCode}");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("PayOS API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                throw new HttpRequestException($"PayOS API error: {response.StatusCode}");
            }

            var result = JsonSerializer.Deserialize<PayOSPaymentResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null)
            {
                throw new InvalidOperationException("Failed to deserialize PayOS response");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting PayOS payment info for order: {OrderCode}", orderCode);
            throw;
        }
    }

    public async Task<bool> VerifyWebhookSignatureAsync(string signature, string payload)
    {
        try
        {
            // Simple signature verification - in production, use proper HMAC verification
            var expectedSignature = await GenerateChecksumAsync(payload);
            return signature.Equals(expectedSignature, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying webhook signature");
            return false;
        }
    }

    public Task<PayOSWebhookData> VerifyWebhookDataAsync(PayOSWebhookPayload webhookPayload)
    {
        try
        {
            // For now, return the data directly - in production, verify signature
            return Task.FromResult(webhookPayload.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying webhook data");
            throw;
        }
    }

    public async Task<string> CancelPaymentLinkAsync(long orderCode, string? cancellationReason = null)
    {
        try
        {
            var payload = new
            {
                cancellationReason = cancellationReason ?? "Cancelled by system"
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/v2/payment-requests/{orderCode}/cancel", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("PayOS API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                throw new HttpRequestException($"PayOS API error: {response.StatusCode}");
            }

            return "CANCELLED";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling payment link for order: {OrderCode}", orderCode);
            throw;
        }
    }

    public async Task<string> ConfirmWebhookAsync(string webhookUrl)
    {
        try
        {
            var payload = new { webhookUrl };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/v2/webhook/confirm", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("PayOS API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                throw new HttpRequestException($"PayOS API error: {response.StatusCode}");
            }

            return "CONFIRMED";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming webhook URL: {WebhookUrl}", webhookUrl);
            throw;
        }
    }

    private Task<string> GenerateChecksumAsync(string data)
    {
        try
        {
            using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(_config.ChecksumKey));
            var hashBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
            return Task.FromResult(Convert.ToHexString(hashBytes).ToLower());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating checksum");
            throw;
        }
    }
}


