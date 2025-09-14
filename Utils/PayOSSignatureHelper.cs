using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Services.Models.Payment;

namespace Utils;

/// <summary>
/// Helper class for PayOS signature validation and testing
/// </summary>
public static class PayOSSignatureHelper
{
    /// <summary>
    /// Test signature validation with sample data from PayOS documentation
    /// </summary>
    /// <param name="checksumKey">The PayOS checksum key</param>
    /// <returns>Test results</returns>
    public static async Task<PayOSSignatureTestResult> TestSignatureValidationAsync(string checksumKey)
    {
        var result = new PayOSSignatureTestResult();
        
        try
        {
            // Sample data from PayOS documentation
            var sampleWebhookData = new PayOSWebhookData
            {
                OrderCode = 123,
                Amount = 3000,
                Description = "VQRIO123",
                AccountNumber = "12345678",
                Reference = "TF230204212323",
                TransactionDateTime = "2023-02-04 18:25:00",
                Currency = "VND",
                PaymentLinkId = "124c33293c43417ab7879e14c8d9eb18",
                Code = "00",
                Desc = "Thành công",
                CounterAccountBankId = "",
                CounterAccountBankName = "",
                CounterAccountName = "",
                CounterAccountNumber = "",
                VirtualAccountName = "",
                VirtualAccountNumber = ""
            };

            var expectedSignature = "412e915d2871504ed31be63c8f62a149a4410d34c4c42affc9006ef9917eaa03";

            // Generate signature
            var generatedSignature = await GenerateSignatureAsync(sampleWebhookData, checksumKey);
            
            // Verify signature
            var isValid = await VerifySignatureAsync(sampleWebhookData, expectedSignature, checksumKey);

            result.IsValid = isValid;
            result.ExpectedSignature = expectedSignature;
            result.GeneratedSignature = generatedSignature;
            result.SampleData = sampleWebhookData;
            result.Success = true;
            result.Message = isValid ? "Signature validation test passed" : "Signature validation test failed";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Error during signature test: {ex.Message}";
            result.Exception = ex;
        }

        return result;
    }

    /// <summary>
    /// Generate signature for PayOS webhook data
    /// </summary>
    /// <param name="data">The webhook data</param>
    /// <param name="checksumKey">The PayOS checksum key</param>
    /// <returns>HMAC SHA256 signature</returns>
    public static async Task<string> GenerateSignatureAsync(PayOSWebhookData data, string checksumKey)
    {
        // Sort data by key alphabetically
        var sortedData = SortObjectByKey(data);
        
        // Convert to query string format: key1=value1&key2=value2
        var queryString = ConvertObjectToQueryString(sortedData);
        
        // Generate HMAC SHA256 signature
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(checksumKey));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(queryString));
        var signature = Convert.ToHexString(hashBytes).ToLower();
        
        return signature;
    }

    /// <summary>
    /// Verify PayOS webhook signature
    /// </summary>
    /// <param name="data">The webhook data</param>
    /// <param name="signature">The signature to verify</param>
    /// <param name="checksumKey">The PayOS checksum key</param>
    /// <returns>True if signature is valid</returns>
    public static async Task<bool> VerifySignatureAsync(PayOSWebhookData data, string signature, string checksumKey)
    {
        var expectedSignature = await GenerateSignatureAsync(data, checksumKey);
        return signature.Equals(expectedSignature, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Sort object properties by key alphabetically
    /// </summary>
    private static Dictionary<string, object> SortObjectByKey(PayOSWebhookData data)
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
    private static string ConvertObjectToQueryString(Dictionary<string, object> data)
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

/// <summary>
/// Result of PayOS signature validation test
/// </summary>
public class PayOSSignatureTestResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string ExpectedSignature { get; set; } = string.Empty;
    public string GeneratedSignature { get; set; } = string.Empty;
    public PayOSWebhookData? SampleData { get; set; }
    public Exception? Exception { get; set; }
}

