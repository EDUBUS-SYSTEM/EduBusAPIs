# Hướng dẫn Test Thanh toán Nhanh

## 🚀 Test Nhanh trong 5 phút

### Bước 1: Chuẩn bị

```bash
# 1. Cài đặt ngrok
npm install -g ngrok

# 2. Chạy ngrok
ngrok http 7000

# 3. Lấy HTTPS URL (ví dụ: https://abc123.ngrok.io)
```

### Bước 2: Cấu hình PayOS

```bash
# Cập nhật User Secrets
cd APIs
dotnet user-secrets set "PayOS:WebhookUrl" "https://your-ngrok-url.ngrok.io/api/payment/webhook/payos"
```

### Bước 3: Chạy Application

```bash
# Chạy API
dotnet run --project APIs
```

### Bước 4: Test với Script

```bash
# Test tạo transaction
.\Scripts\TestRealPayment.ps1 -JwtToken "your-jwt-token" -CreateTransaction

# Test tạo QR code
.\Scripts\TestRealPayment.ps1 -JwtToken "your-jwt-token" -GenerateQR

# Test webhook
.\Scripts\TestRealPayment.ps1 -JwtToken "your-jwt-token" -TestWebhook
```

## 📱 Test với Mobile App

### 1. Tạo Transaction

```bash
curl -X POST "https://localhost:7223/api/payment" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "pickupPointRequestId": "PPR_TEST_001",
    "scheduleId": "schedule-guid"
  }'
```

### 2. Tạo QR Code

```bash
curl -X POST "https://localhost:7223/api/payment/{transactionId}/qrcode" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### 3. Quét QR Code

- Mở app ngân hàng
- Quét QR code
- Nhập số tiền chính xác
- Xác nhận thanh toán

### 4. Kiểm tra Kết quả

```bash
# Kiểm tra transaction status
curl -X GET "https://localhost:7223/api/payment/{transactionId}" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

## 🔍 Test Scenarios

### ✅ Test Case 1: Thanh toán thành công

1. Tạo transaction với số tiền 10,000 VND
2. Tạo QR code
3. Quét QR code và thanh toán đúng số tiền
4. Kiểm tra webhook được gọi
5. Kiểm tra transaction status = "Paid"

### ❌ Test Case 2: Thanh toán thất bại

1. Tạo transaction với số tiền 10,000 VND
2. Tạo QR code
3. Quét QR code và thanh toán sai số tiền
4. Kiểm tra không có webhook
5. Kiểm tra transaction status = "Notyet"

### 🚫 Test Case 3: Hủy thanh toán

1. Tạo transaction với số tiền 10,000 VND
2. Tạo QR code
3. Quét QR code và hủy thanh toán
4. Kiểm tra webhook với status "cancelled"
5. Kiểm tra transaction status

## 🛠️ Troubleshooting

### Webhook không được gọi

```bash
# Kiểm tra ngrok
curl -X GET "http://localhost:4040/api/tunnels"

# Kiểm tra webhook URL
curl -X POST "https://your-ngrok-url.ngrok.io/api/payment/webhook/payos" \
  -H "Content-Type: application/json" \
  -d '{"test": "data"}'
```

### Signature verification failed

```bash
# Test signature generation
curl -X POST "https://localhost:7223/api/PayOSTest/test-signature" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Transaction không được cập nhật

```bash
# Kiểm tra logs
dotnet run --project APIs --verbosity detailed

# Kiểm tra database
# Xem trong SQL Server Management Studio hoặc MongoDB Compass
```

## 📊 Monitoring

### Application Logs

```bash
# Xem logs real-time
tail -f logs/application.log

# Hoặc sử dụng dotnet run với verbosity
dotnet run --project APIs --verbosity detailed
```

### ngrok Dashboard

- Truy cập: http://localhost:4040
- Xem tab "Requests"
- Kiểm tra webhook requests

### PayOS Dashboard

- Truy cập: https://dev.payos.vn/ (development)
- Xem Transactions, Webhooks, API Usage

## 🚨 Common Issues

### 1. "Invalid signature"

- Kiểm tra checksum key trong User Secrets
- Kiểm tra data format
- Kiểm tra encoding (UTF-8)

### 2. "Webhook not received"

- Kiểm tra ngrok đang chạy
- Kiểm tra webhook URL trong PayOS dashboard
- Kiểm tra firewall settings

### 3. "Transaction not found"

- Kiểm tra transaction ID
- Kiểm tra database connection
- Kiểm tra transaction có tồn tại không

## 📋 Checklist

### Pre-Testing

- [ ] PayOS account setup
- [ ] API credentials configured
- [ ] ngrok running
- [ ] Application running
- [ ] Webhook URL configured

### Testing

- [ ] Create transaction
- [ ] Generate QR code
- [ ] Test successful payment
- [ ] Test failed payment
- [ ] Test cancelled payment
- [ ] Verify webhook received
- [ ] Check database updated

### Post-Testing

- [ ] All test cases passed
- [ ] No errors in logs
- [ ] Database consistent
- [ ] Ready for production

## 🎯 Production Checklist

### Security

- [ ] Remove test controllers
- [ ] Use environment variables
- [ ] Enable HTTPS only
- [ ] Configure rate limiting

### Monitoring

- [ ] Set up logging
- [ ] Configure alerts
- [ ] Monitor webhook delivery
- [ ] Track payment success rate

### Backup

- [ ] Backup transaction data
- [ ] Test restore procedures
- [ ] Document recovery process

## 📞 Support

### PayOS Support

- Email: support@payos.vn
- Documentation: https://payos.vn/docs/
- Developer Portal: https://dev.payos.vn/

### Application Support

- Check logs first
- Use test endpoints
- Verify configuration
- Test with sample data

---

**Lưu ý**: Luôn test với số tiền nhỏ trước khi test với số tiền lớn trong production!

