using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.Contracts;
using Services.Models.Payment;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

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
            // Build signature per PayOS docs:
            // data = amount=$amount&cancelUrl=$cancelUrl&description=$description&orderCode=$orderCode&returnUrl=$returnUrl
            var signature = GenerateCreatePaymentSignature(
                request.Amount,
                request.CancelUrl,
                request.Description,
                request.OrderCode,
                request.ReturnUrl);

            var payload = new
            {
                orderCode = request.OrderCode,
                amount = request.Amount,
                description = request.Description,
                items = request.Items,
                returnUrl = request.ReturnUrl,
                cancelUrl = request.CancelUrl,
                signature = signature
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

    private string GenerateCreatePaymentSignature(int amount, string cancelUrl, string description, long orderCode, string returnUrl)
    {
        // Build canonical string in alphabetical key order
        // amount=...&cancelUrl=...&description=...&orderCode=...&returnUrl=...
        var data = $"amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_config.ChecksumKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
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
            var expectedSignature = await GenerateChecksumAsync(payload);
            return signature.Equals(expectedSignature, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying webhook signature");
            return false;
        }
    }

    public async Task<PayOSWebhookData> VerifyWebhookDataAsync(PayOSWebhookPayload webhookPayload)
    {
        try
        {
            // Verify signature first
            var isValid = await VerifyPayOSWebhookSignatureAsync(webhookPayload.Data, webhookPayload.Signature);
            if (!isValid)
            {
                _logger.LogWarning("Invalid webhook signature for order code: {OrderCode}", webhookPayload.Data.OrderCode);
                throw new UnauthorizedAccessException("Invalid webhook signature");
            }

            return webhookPayload.Data;
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
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_config.ChecksumKey));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Task.FromResult(Convert.ToHexString(hashBytes).ToLower());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating checksum");
            throw;
        }
    }

    /// <summary>
    /// Generate signature for PayOS webhook data according to PayOS documentation
    /// </summary>
    /// <param name="data">The data object to sign</param>
    /// <returns>HMAC SHA256 signature</returns>
    public async Task<string> GenerateSignatureAsync(PayOSWebhookData data)
    {
        try
        {
            // Sort data by key alphabetically
            var sortedData = SortObjectByKey(data);
            
            // Convert to query string format: key1=value1&key2=value2
            var queryString = ConvertObjectToQueryString(sortedData);
            
            _logger.LogDebug("Data to sign: {QueryString}", queryString);
            
            // Generate HMAC SHA256 signature
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_config.ChecksumKey));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(queryString));
            var signature = Convert.ToHexString(hashBytes).ToLower();
            
            _logger.LogDebug("Generated signature: {Signature}", signature);
            
            return signature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating signature for PayOS webhook data");
            throw;
        }
    }

    /// <summary>
    /// Verify PayOS webhook signature according to PayOS documentation
    /// </summary>
    /// <param name="data">The webhook data</param>
    /// <param name="signature">The signature to verify</param>
    /// <returns>True if signature is valid</returns>
    public async Task<bool> VerifyPayOSWebhookSignatureAsync(PayOSWebhookData data, string signature)
    {
        try
        {
            var expectedSignature = await GenerateSignatureAsync(data);
            var isValid = signature.Equals(expectedSignature, StringComparison.OrdinalIgnoreCase);
            
            _logger.LogDebug("Signature verification: Expected={Expected}, Received={Received}, Valid={Valid}", 
                expectedSignature, signature, isValid);
            
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying PayOS webhook signature");
            return false;
        }
    }

    /// <summary>
    /// Sort object properties by key alphabetically
    /// </summary>
    private Dictionary<string, object> SortObjectByKey(PayOSWebhookData data)
    {
        var dict = new Dictionary<string, object>
        {
            ["orderCode"] = data.OrderCode,
            ["amount"] = data.Amount,
            ["description"] = data.Description ?? "",
            ["accountNumber"] = data.AccountNumber ?? "",
            ["reference"] = data.Reference ?? "",
            ["transactionDateTime"] = data.TransactionDateTime ?? "",
            ["currency"] = data.Currency ?? "",
            ["paymentLinkId"] = data.PaymentLinkId ?? "",
            ["code"] = data.Code ?? "",
            ["desc"] = data.Desc ?? "",
            ["counterAccountBankId"] = data.CounterAccountBankId ?? "",
            ["counterAccountBankName"] = data.CounterAccountBankName ?? "",
            ["counterAccountName"] = data.CounterAccountName ?? "",
            ["counterAccountNumber"] = data.CounterAccountNumber ?? "",
            ["virtualAccountName"] = data.VirtualAccountName ?? "",
            ["virtualAccountNumber"] = data.VirtualAccountNumber ?? ""
        };

        return dict.OrderBy(kvp => kvp.Key).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Convert object to query string format
    /// </summary>
    private string ConvertObjectToQueryString(Dictionary<string, object> data)
    {
        var queryParts = new List<string>();
        
        foreach (var kvp in data)
        {
            var value = kvp.Value?.ToString() ?? "";
            
            // Handle null/undefined values as empty string
            if (value == "null" || value == "undefined")
            {
                value = "";
            }
            
            queryParts.Add($"{kvp.Key}={value}");
        }
        
        return string.Join("&", queryParts);
    }
}


