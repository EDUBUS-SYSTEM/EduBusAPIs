# PayOS Signature Validation Implementation

## Tổng quan

Tài liệu này mô tả cách implement signature validation cho PayOS webhook theo hướng dẫn chính thức của PayOS.

## Cấu trúc Implementation

### 1. PayOSService.cs

- **GenerateSignatureAsync**: Tạo signature cho PayOS webhook data
- **VerifyPayOSWebhookSignatureAsync**: Xác thực signature từ PayOS webhook
- **SortObjectByKey**: Sắp xếp object theo key alphabetically
- **ConvertObjectToQueryString**: Chuyển đổi object thành query string format

### 2. PayOSSignatureHelper.cs

- Utility class để test signature validation
- **TestSignatureValidationAsync**: Test với sample data từ PayOS documentation
- **GenerateSignatureAsync**: Generate signature với custom data
- **VerifySignatureAsync**: Verify signature với custom data

### 3. PayOSTestController.cs

- Test controller để kiểm tra signature validation
- **test-signature**: Test với sample data
- **test-signature-custom**: Test với custom data
- **test-signature-verify**: Test signature verification
- **config**: Lấy PayOS configuration (không có sensitive data)

## Thuật toán Signature Validation

### Bước 1: Sắp xếp dữ liệu

```csharp
// Sắp xếp các field theo thứ tự alphabet
var sortedData = new Dictionary<string, object>
{
    ["accountNumber"] = data.AccountNumber ?? "",
    ["amount"] = data.Amount,
    ["code"] = data.Code ?? "",
    ["counterAccountBankId"] = data.CounterAccountBankId ?? "",
    ["counterAccountBankName"] = data.CounterAccountBankName ?? "",
    ["counterAccountName"] = data.CounterAccountName ?? "",
    ["counterAccountNumber"] = data.CounterAccountNumber ?? "",
    ["currency"] = data.Currency ?? "",
    ["desc"] = data.Desc ?? "",
    ["description"] = data.Description ?? "",
    ["orderCode"] = data.OrderCode,
    ["paymentLinkId"] = data.PaymentLinkId ?? "",
    ["reference"] = data.Reference ?? "",
    ["transactionDateTime"] = data.TransactionDateTime ?? "",
    ["virtualAccountName"] = data.VirtualAccountName ?? "",
    ["virtualAccountNumber"] = data.VirtualAccountNumber ?? ""
};
```

### Bước 2: Chuyển đổi thành Query String

```csharp
// Chuyển đổi thành format: key1=value1&key2=value2
var queryString = "accountNumber=12345678&amount=3000&code=00&counterAccountBankId=&counterAccountBankName=&counterAccountName=&counterAccountNumber=&currency=VND&desc=Thành công&description=VQRIO123&orderCode=123&paymentLinkId=124c33293c43417ab7879e14c8d9eb18&reference=TF230204212323&transactionDateTime=2023-02-04 18:25:00&virtualAccountName=&virtualAccountNumber=";
```

### Bước 3: Tạo HMAC SHA256 Signature

```csharp
using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(checksumKey));
var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(queryString));
var signature = Convert.ToHexString(hashBytes).ToLower();
```

## Sample Data từ PayOS Documentation

```json
{
  "code": "00",
  "desc": "success",
  "success": true,
  "data": {
    "orderCode": 123,
    "amount": 3000,
    "description": "VQRIO123",
    "accountNumber": "12345678",
    "reference": "TF230204212323",
    "transactionDateTime": "2023-02-04 18:25:00",
    "currency": "VND",
    "paymentLinkId": "124c33293c43417ab7879e14c8d9eb18",
    "code": "00",
    "desc": "Thành công",
    "counterAccountBankId": "",
    "counterAccountBankName": "",
    "counterAccountName": "",
    "counterAccountNumber": "",
    "virtualAccountName": "",
    "virtualAccountNumber": ""
  },
  "signature": "412e915d2871504ed31be63c8f62a149a4410d34c4c42affc9006ef9917eaa03"
}
```

## Cách Test

### 1. Test với Sample Data

```bash
POST /api/PayOSTest/test-signature
Authorization: Bearer YOUR_JWT_TOKEN
```

### 2. Test với Custom Data

```bash
POST /api/PayOSTest/test-signature-custom
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "orderCode": 123,
  "amount": 3000,
  "description": "VQRIO123",
  "accountNumber": "12345678",
  "reference": "TF230204212323",
  "transactionDateTime": "2023-02-04 18:25:00",
  "currency": "VND",
  "paymentLinkId": "124c33293c43417ab7879e14c8d9eb18",
  "code": "00",
  "desc": "Thành công",
  "counterAccountBankId": "",
  "counterAccountBankName": "",
  "counterAccountName": "",
  "counterAccountNumber": "",
  "virtualAccountName": "",
  "virtualAccountNumber": ""
}
```

### 3. Test Signature Verification

```bash
POST /api/PayOSTest/test-signature-verify
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "data": {
    "orderCode": 123,
    "amount": 3000,
    "description": "VQRIO123",
    "accountNumber": "12345678",
    "reference": "TF230204212323",
    "transactionDateTime": "2023-02-04 18:25:00",
    "currency": "VND",
    "paymentLinkId": "124c33293c43417ab7879e14c8d9eb18",
    "code": "00",
    "desc": "Thành công",
    "counterAccountBankId": "",
    "counterAccountBankName": "",
    "counterAccountName": "",
    "counterAccountNumber": "",
    "virtualAccountName": "",
    "virtualAccountNumber": ""
  },
  "signature": "412e915d2871504ed31be63c8f62a149a4410d34c4c42affc9006ef9917eaa03"
}
```

## Webhook Implementation

### PaymentController.cs

```csharp
[HttpPost("webhook/payos")]
[AllowAnonymous]
public async Task<IActionResult> HandlePayOSWebhook([FromBody] PayOSWebhookPayload payload)
{
    try
    {
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
```

### PaymentService.cs

```csharp
public async Task<bool> HandlePayOSWebhookAsync(PayOSWebhookPayload payload)
{
    // Verify webhook signature first
    var isValidSignature = await _payOSService.VerifyPayOSWebhookSignatureAsync(payload.Data, payload.Signature);
    if (!isValidSignature)
    {
        _logger.LogWarning("Invalid webhook signature for order code: {OrderCode}", payload.Data.OrderCode);
        return false;
    }

    // Process webhook data...
}
```

## Lưu ý Quan trọng

1. **Checksum Key**: Phải được lưu trữ an toàn trong User Secrets hoặc Environment Variables
2. **Signature Verification**: Luôn verify signature trước khi xử lý webhook data
3. **Null Values**: Các giá trị null/undefined được chuyển thành empty string
4. **Case Sensitivity**: Signature comparison không phân biệt hoa thường
5. **Encoding**: Sử dụng UTF-8 encoding cho tất cả operations

## Security Best Practices

1. **Never log sensitive data**: Không log checksum key hoặc signature
2. **Validate all inputs**: Validate tất cả input từ webhook
3. **Rate limiting**: Implement rate limiting cho webhook endpoints
4. **HTTPS only**: Chỉ sử dụng HTTPS cho webhook URLs
5. **Idempotency**: Implement idempotency để tránh duplicate processing

## Troubleshooting

### Signature Verification Failed

1. Kiểm tra checksum key có đúng không
2. Kiểm tra data format có đúng không
3. Kiểm tra encoding (UTF-8)
4. Kiểm tra thứ tự sắp xếp key

### Webhook Not Received

1. Kiểm tra webhook URL có accessible không
2. Kiểm tra firewall settings
3. Kiểm tra PayOS dashboard configuration
4. Sử dụng ngrok cho local development

## Production Deployment

1. **Remove test controllers**: Xóa PayOSTestController trong production
2. **Environment variables**: Sử dụng environment variables cho sensitive data
3. **Monitoring**: Implement monitoring cho webhook processing
4. **Logging**: Implement proper logging cho debugging
5. **Error handling**: Implement proper error handling và retry logic

