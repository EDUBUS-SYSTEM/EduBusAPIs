using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.Contracts;
using Services.Implementations;
using Services.Models.Payment;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Tests;

/// <summary>
/// Unit tests for PayOS signature validation
/// </summary>
public class PayOSSignatureValidationTests
{
    private readonly IPayOSService _payOSService;
    private readonly PayOSConfig _config;

    public PayOSSignatureValidationTests()
    {
        // Test configuration
        _config = new PayOSConfig
        {
            ClientId = "test-client-id",
            ApiKey = "test-api-key",
            ChecksumKey = "1a54716c8f0efb2744fb28b6e38b25da7f67a925d98bc1c18bd8faaecadd7675", // Sample key from PayOS docs
            BaseUrl = "https://api-merchant.payos.vn",
            WebhookUrl = "https://test.com/webhook",
            ReturnUrl = "https://test.com/return",
            CancelUrl = "https://test.com/cancel",
            QrExpirationMinutes = 15
        };

        var logger = new Mock<ILogger<PayOSService>>();
        var httpClient = new HttpClient();
        var options = Options.Create(_config);

        _payOSService = new PayOSService(options, httpClient, logger.Object);
    }

    [Fact]
    public async Task GenerateSignature_WithSampleData_ShouldReturnCorrectSignature()
    {
        // Arrange
        var sampleData = new PayOSWebhookData
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

        // Act
        var generatedSignature = await _payOSService.GenerateSignatureAsync(sampleData);

        // Assert
        Assert.Equal(expectedSignature, generatedSignature);
    }

    [Fact]
    public async Task VerifySignature_WithValidSignature_ShouldReturnTrue()
    {
        // Arrange
        var sampleData = new PayOSWebhookData
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

        var validSignature = "412e915d2871504ed31be63c8f62a149a4410d34c4c42affc9006ef9917eaa03";

        // Act
        var isValid = await _payOSService.VerifyPayOSWebhookSignatureAsync(sampleData, validSignature);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public async Task VerifySignature_WithInvalidSignature_ShouldReturnFalse()
    {
        // Arrange
        var sampleData = new PayOSWebhookData
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

        var invalidSignature = "invalid_signature_123456789";

        // Act
        var isValid = await _payOSService.VerifyPayOSWebhookSignatureAsync(sampleData, invalidSignature);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task GenerateSignature_WithNullValues_ShouldHandleCorrectly()
    {
        // Arrange
        var sampleData = new PayOSWebhookData
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
            CounterAccountBankId = null,
            CounterAccountBankName = null,
            CounterAccountName = null,
            CounterAccountNumber = null,
            VirtualAccountName = null,
            VirtualAccountNumber = null
        };

        // Act
        var signature = await _payOSService.GenerateSignatureAsync(sampleData);

        // Assert
        Assert.NotEmpty(signature);
        Assert.Equal(64, signature.Length); // SHA256 hex string length
    }

    [Fact]
    public async Task GenerateSignature_WithDifferentData_ShouldReturnDifferentSignatures()
    {
        // Arrange
        var data1 = new PayOSWebhookData
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

        var data2 = new PayOSWebhookData
        {
            OrderCode = 124, // Different order code
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

        // Act
        var signature1 = await _payOSService.GenerateSignatureAsync(data1);
        var signature2 = await _payOSService.GenerateSignatureAsync(data2);

        // Assert
        Assert.NotEqual(signature1, signature2);
    }

    [Fact]
    public async Task GenerateSignature_WithEmptyStrings_ShouldHandleCorrectly()
    {
        // Arrange
        var sampleData = new PayOSWebhookData
        {
            OrderCode = 123,
            Amount = 3000,
            Description = "",
            AccountNumber = "",
            Reference = "",
            TransactionDateTime = "",
            Currency = "",
            PaymentLinkId = "",
            Code = "",
            Desc = "",
            CounterAccountBankId = "",
            CounterAccountBankName = "",
            CounterAccountName = "",
            CounterAccountNumber = "",
            VirtualAccountName = "",
            VirtualAccountNumber = ""
        };

        // Act
        var signature = await _payOSService.GenerateSignatureAsync(sampleData);

        // Assert
        Assert.NotEmpty(signature);
        Assert.Equal(64, signature.Length); // SHA256 hex string length
    }
}

/// <summary>
/// Mock logger for testing
/// </summary>
public class Mock<T> where T : class
{
    public T Object { get; } = Activator.CreateInstance<T>();
}

