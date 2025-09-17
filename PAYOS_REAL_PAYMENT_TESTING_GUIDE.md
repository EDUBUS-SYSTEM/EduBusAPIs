# Hướng dẫn Test Thanh toán Thực tế với PayOS

## Tổng quan

Hướng dẫn này mô tả cách test thanh toán thực tế với PayOS từ môi trường development đến production, bao gồm cả test với tiền thật và sandbox.

## 1. Chuẩn bị Môi trường

### 1.1 PayOS Account Setup

#### Development/Sandbox

1. **Đăng ký tài khoản PayOS Developer**

   - Truy cập: https://dev.payos.vn/
   - Đăng ký tài khoản developer
   - Xác thực email và thông tin

2. **Lấy API Credentials**
   - Vào Dashboard → API Keys
   - Lấy các thông tin:
     - `ClientId`: Client ID của bạn
     - `ApiKey`: API Key
     - `ChecksumKey`: Checksum Key (dùng để verify signature)

#### Production

1. **Đăng ký tài khoản PayOS Merchant**

   - Truy cập: https://my.payos.vn/
   - Hoàn thành quy trình xác thực merchant
   - Cung cấp giấy tờ pháp lý

2. **Lấy Production Credentials**
   - Tương tự như development nhưng với credentials production

### 1.2 Cấu hình Local Environment

#### User Secrets (Development)

```bash
# Khởi tạo User Secrets
cd APIs
dotnet user-secrets init

# Cấu hình PayOS Development
dotnet user-secrets set "PayOS:ClientId" "your-dev-client-id"
dotnet user-secrets set "PayOS:ApiKey" "your-dev-api-key"
dotnet user-secrets set "PayOS:ChecksumKey" "your-dev-checksum-key"
dotnet user-secrets set "PayOS:BaseUrl" "https://api-merchant.payos.vn"
dotnet user-secrets set "PayOS:WebhookUrl" "https://your-ngrok-url.ngrok.io/api/payment/webhook/payos"
dotnet user-secrets set "PayOS:ReturnUrl" "https://localhost:7000/api/payment/return"
dotnet user-secrets set "PayOS:CancelUrl" "https://localhost:7000/api/payment/cancel"
dotnet user-secrets set "PayOS:QrExpirationMinutes" "15"
```

#### Environment Variables (Production)

```bash
# Production Environment Variables
export PayOS__ClientId="prod-client-id"
export PayOS__ApiKey="prod-api-key"
export PayOS__ChecksumKey="prod-checksum-key"
export PayOS__BaseUrl="https://api-merchant.payos.vn"
export PayOS__WebhookUrl="https://yourdomain.com/api/payment/webhook/payos"
export PayOS__ReturnUrl="https://yourdomain.com/api/payment/return"
export PayOS__CancelUrl="https://yourdomain.com/api/payment/cancel"
export PayOS__QrExpirationMinutes="15"
```

## 2. Setup ngrok cho Local Development

### 2.1 Cài đặt ngrok

```bash
# Cài đặt ngrok
npm install -g ngrok

# Hoặc download từ https://ngrok.com/download
```

### 2.2 Chạy ngrok

```bash
# Expose local server port 7000
ngrok http 7000

# Lấy HTTPS URL (ví dụ: https://abc123.ngrok.io)
# Sử dụng URL này cho PayOS webhook configuration
```

### 2.3 Cấu hình PayOS Webhook

1. Vào PayOS Dashboard → Webhook Settings
2. Cập nhật Webhook URL: `https://your-ngrok-url.ngrok.io/api/payment/webhook/payos`
3. Lưu cấu hình

## 3. Test Flow Thanh toán

### 3.1 Tạo Transaction

#### API Endpoint

```http
POST /api/payment
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "pickupPointRequestId": "PPR_20240115_001",
  "scheduleId": "schedule-guid-here"
}
```

#### Response

```json
{
  "id": "transaction-guid",
  "transactionCode": "TXN_20240115120000_PPR_20240115_001",
  "status": "Notyet",
  "amount": 150000,
  "currency": "VND",
  "description": "Phí vận chuyển học sinh - Yêu cầu điểm đón PPR_20240115_001",
  "provider": "PayOS",
  "createdAt": "2024-01-15T12:00:00Z"
}
```

### 3.2 Tạo QR Code

#### API Endpoint

```http
POST /api/payment/{transactionId}/qrcode
Authorization: Bearer YOUR_JWT_TOKEN
```

#### Response

```json
{
  "qrCode": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAA...",
  "checkoutUrl": "https://pay.payos.vn/web/123456789",
  "expiresAt": "2024-01-15T12:15:00Z"
}
```

### 3.3 Test Thanh toán

#### Cách 1: Sử dụng QR Code

1. **Quét QR Code** bằng app ngân hàng
2. **Nhập số tiền** chính xác
3. **Xác nhận thanh toán**
4. **Kiểm tra webhook** được gọi

#### Cách 2: Sử dụng Checkout URL

1. **Mở Checkout URL** trong browser
2. **Chọn phương thức thanh toán**
3. **Nhập thông tin thanh toán**
4. **Xác nhận thanh toán**

## 4. Test Scenarios

### 4.1 Test Case 1: Thanh toán thành công

#### Bước 1: Tạo transaction

```bash
curl -X POST "https://localhost:7223/api/payment" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "pickupPointRequestId": "PPR_TEST_001",
    "scheduleId": "schedule-guid"
  }'
```

#### Bước 2: Tạo QR code

```bash
curl -X POST "https://localhost:7223/api/payment/{transactionId}/qrcode" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

#### Bước 3: Thanh toán

- Quét QR code hoặc sử dụng checkout URL
- Thanh toán với số tiền chính xác
- Xác nhận thanh toán

#### Bước 4: Kiểm tra kết quả

```bash
# Kiểm tra transaction status
curl -X GET "https://localhost:7223/api/payment/{transactionId}" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Kiểm tra transaction events
curl -X GET "https://localhost:7223/api/payment/{transactionId}/events" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### 4.2 Test Case 2: Thanh toán thất bại

#### Bước 1-2: Tương tự Test Case 1

#### Bước 3: Thanh toán thất bại

- Quét QR code
- Nhập sai số tiền (ít hơn hoặc nhiều hơn)
- Hoặc hủy thanh toán

#### Bước 4: Kiểm tra kết quả

- Transaction status vẫn là "Notyet" hoặc "Failed"
- Không có webhook được gọi

### 4.3 Test Case 3: Hủy thanh toán

#### Bước 1-2: Tương tự Test Case 1

#### Bước 3: Hủy thanh toán

- Quét QR code
- Nhấn "Hủy" hoặc đóng app
- Hoặc sử dụng Cancel URL

#### Bước 4: Kiểm tra kết quả

- Transaction status vẫn là "Notyet"
- Có thể có webhook với status "cancelled"

## 5. Test Webhook

### 5.1 Kiểm tra Webhook Logs

#### Trong Application Logs

```bash
# Xem logs của application
dotnet run --project APIs

# Hoặc sử dụng logging framework
tail -f logs/application.log
```

#### Trong ngrok Dashboard

1. Truy cập: http://localhost:4040
2. Xem tab "Requests"
3. Kiểm tra webhook requests

### 5.2 Test Webhook Manually

#### Simulate PayOS Webhook

```bash
curl -X POST "https://localhost:7223/api/payment/webhook/payos" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "00",
    "desc": "success",
    "success": true,
    "data": {
      "orderCode": 123456789,
      "amount": 150000,
      "description": "Test payment",
      "accountNumber": "1234567890",
      "reference": "TXN_20240115_001",
      "transactionDateTime": "2024-01-15T12:05:00",
      "currency": "VND",
      "paymentLinkId": "pay_123456789",
      "code": "00",
      "desc": "Thành công",
      "counterAccountBankId": "",
      "counterAccountBankName": "",
      "counterAccountName": "",
      "counterAccountNumber": "",
      "virtualAccountName": "",
      "virtualAccountNumber": ""
    },
    "signature": "generated-signature-here"
  }'
```

## 6. Test với Tiền Thật (Production)

### 6.1 Chuẩn bị

#### Điều kiện

- Có tài khoản PayOS production
- Có tài khoản ngân hàng để test
- Có domain và SSL certificate

#### Cấu hình Production

```bash
# Production Environment Variables
export PayOS__ClientId="prod-client-id"
export PayOS__ApiKey="prod-api-key"
export PayOS__ChecksumKey="prod-checksum-key"
export PayOS__WebhookUrl="https://yourdomain.com/api/payment/webhook/payos"
export PayOS__ReturnUrl="https://yourdomain.com/api/payment/return"
export PayOS__CancelUrl="https://yourdomain.com/api/payment/cancel"
```

### 6.2 Test với Số tiền Nhỏ

#### Khuyến nghị

- Bắt đầu với số tiền nhỏ (10,000 VND)
- Test với 1-2 giao dịch
- Kiểm tra kỹ webhook và database

#### Quy trình

1. **Tạo transaction** với số tiền nhỏ
2. **Thanh toán** bằng app ngân hàng
3. **Kiểm tra webhook** được gọi
4. **Verify database** được cập nhật
5. **Kiểm tra email/SMS** notification (nếu có)

### 6.3 Test với Số tiền Lớn

#### Sau khi test số tiền nhỏ thành công

- Test với số tiền lớn hơn (100,000 VND)
- Test với nhiều giao dịch
- Test với các phương thức thanh toán khác nhau

## 7. Monitoring và Debugging

### 7.1 Application Logs

#### Cấu hình Logging

```json
// appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Services.Implementations.PayOSService": "Debug",
      "Services.Implementations.PaymentService": "Debug"
    }
  }
}
```

#### Log Messages

```csharp
_logger.LogInformation("PayOS payment created: OrderCode={OrderCode}, Amount={Amount}",
    orderCode, amount);

_logger.LogInformation("PayOS webhook received: OrderCode={OrderCode}, Signature={Signature}",
    payload.Data.OrderCode, payload.Signature);

_logger.LogWarning("Invalid webhook signature for order code: {OrderCode}",
    payload.Data.OrderCode);
```

### 7.2 Database Monitoring

#### Kiểm tra Transaction Table

```sql
-- Kiểm tra transactions
SELECT * FROM Transactions
WHERE CreatedAt >= DATEADD(day, -1, GETDATE())
ORDER BY CreatedAt DESC;

-- Kiểm tra payment events
SELECT * FROM PaymentEventLogs
WHERE TransactionId = 'your-transaction-id'
ORDER BY AtUtc DESC;
```

### 7.3 PayOS Dashboard

#### Kiểm tra trong PayOS Dashboard

1. **Transactions**: Xem danh sách giao dịch
2. **Webhooks**: Xem webhook delivery logs
3. **API Usage**: Xem API usage statistics
4. **Errors**: Xem error logs

## 8. Troubleshooting

### 8.1 Common Issues

#### Webhook không được gọi

1. **Kiểm tra ngrok**: Đảm bảo ngrok đang chạy
2. **Kiểm tra URL**: Đảm bảo webhook URL đúng
3. **Kiểm tra firewall**: Đảm bảo port 7000 accessible
4. **Kiểm tra PayOS config**: Đảm bảo webhook URL đã được cấu hình

#### Signature verification failed

1. **Kiểm tra checksum key**: Đảm bảo key đúng
2. **Kiểm tra data format**: Đảm bảo data format đúng
3. **Kiểm tra encoding**: Đảm bảo UTF-8 encoding
4. **Kiểm tra thứ tự key**: Đảm bảo key được sắp xếp alphabetically

#### Transaction không được cập nhật

1. **Kiểm tra webhook**: Đảm bảo webhook được gọi
2. **Kiểm tra database**: Đảm bảo database connection
3. **Kiểm tra logs**: Xem error logs
4. **Kiểm tra PayOS status**: Đảm bảo PayOS service hoạt động

### 8.2 Debug Commands

#### Test Signature Generation

```bash
# Test signature generation
curl -X POST "https://localhost:7223/api/PayOSTest/test-signature" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

#### Test Webhook Endpoint

```bash
# Test webhook endpoint
curl -X POST "https://localhost:7223/api/payment/webhook/payos" \
  -H "Content-Type: application/json" \
  -d '{"test": "data"}'
```

#### Check Application Health

```bash
# Health check
curl -X GET "https://localhost:7223/health"
```

## 9. Best Practices

### 9.1 Security

- ✅ **Never log sensitive data**: Không log API keys, checksum keys
- ✅ **Use HTTPS**: Chỉ sử dụng HTTPS cho production
- ✅ **Validate all inputs**: Validate tất cả input từ webhook
- ✅ **Rate limiting**: Implement rate limiting cho webhook endpoints

### 9.2 Testing

- ✅ **Test với số tiền nhỏ trước**: Bắt đầu với 10,000 VND
- ✅ **Test multiple scenarios**: Success, failure, cancellation
- ✅ **Test với different payment methods**: QR, bank transfer, etc.
- ✅ **Monitor logs**: Theo dõi logs trong quá trình test

### 9.3 Production

- ✅ **Remove test controllers**: Xóa PayOSTestController trong production
- ✅ **Use environment variables**: Sử dụng environment variables cho sensitive data
- ✅ **Implement monitoring**: Set up monitoring và alerting
- ✅ **Backup data**: Backup transaction data regularly

## 10. Checklist

### 10.1 Pre-Testing

- [ ] PayOS account setup (development/production)
- [ ] API credentials configured
- [ ] ngrok setup (for local development)
- [ ] Webhook URL configured in PayOS
- [ ] Application running và accessible

### 10.2 Testing

- [ ] Create transaction successfully
- [ ] Generate QR code successfully
- [ ] Payment with correct amount
- [ ] Payment with wrong amount
- [ ] Payment cancellation
- [ ] Webhook received và processed
- [ ] Database updated correctly
- [ ] Signature verification working

### 10.3 Post-Testing

- [ ] All test cases passed
- [ ] No errors in logs
- [ ] Database data consistent
- [ ] PayOS dashboard shows correct data
- [ ] Ready for production deployment

## Kết luận

Hướng dẫn này cung cấp quy trình test thanh toán thực tế với PayOS từ development đến production. Đảm bảo test kỹ lưỡng trước khi deploy production để tránh các vấn đề về tài chính và bảo mật.

