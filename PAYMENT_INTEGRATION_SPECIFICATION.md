# Payment Integration Specification for Student Status Management

## 📋 Business Requirements Analysis

### Current Business Flow:
1. **Initial Registration**: Parent submits pickup point request → Admin approves → Student becomes `Active`
2. **Payment Integration**: Student pays for service → Status remains `Active` 
3. **Renewal Process**: Parent renews subscription → Status remains `Active`
4. **Expiration Handling**: Service expires → Status becomes `Inactive`

## 🎯 Status Management Logic

### Current Status Definitions:
```csharp
public enum StudentStatus
{
    Available = 0,   // Student created but no service request yet
    Pending = 1,     // Service request submitted, waiting for approval  
    Active = 2,      // Approved and actively using service (OR paid)
    Inactive = 3,    // Temporarily stopped using service
    Deleted = 4      // Soft-deleted by admin
}
```

### Status Transition Rules:

| **From** | **To** | **Trigger** | **Business Logic** |
|----------|--------|-------------|-------------------|
| `Available` | `Pending` | Parent submits request | First-time registration |
| `Pending` | `Active` | Admin approves | Initial approval |
| `Available` | `Active` | Payment made | Direct payment (skip approval) |
| `Active` | `Active` | Payment renewal | Subscription renewal |
| `Active` | `Inactive` | Service expires | Auto-deactivation |
| `Inactive` | `Active` | Payment made | Reactivation after payment |
| Any | `Deleted` | Admin action | Soft delete |

## 🔧 Technical Implementation

### 1. Payment Service Integration Points

#### A. Initial Payment (First-time users)
```csharp
// When parent pays for first-time service
public async Task ProcessInitialPaymentAsync(Guid studentId, PaymentRequest request)
{
    // Process payment
    var paymentResult = await ProcessPayment(request);
    
    if (paymentResult.Success)
    {
        // Auto-activate student (skip approval if needed)
        await _studentService.ActivateStudentByPaymentAsync(studentId);
        
        // Create transport fee record
        await CreateTransportFeeRecord(studentId, paymentResult.Amount);
    }
}
```

#### B. Renewal Payment (Existing users)
```csharp
// When parent renews subscription
public async Task ProcessRenewalPaymentAsync(Guid studentId, PaymentRequest request)
{
    // Process payment
    var paymentResult = await ProcessPayment(request);
    
    if (paymentResult.Success)
    {
        // Reactivate if currently inactive
        var student = await _studentService.GetStudentByIdAsync(studentId);
        if (student.Status == StudentStatus.Inactive)
        {
            await _studentService.ActivateStudentByPaymentAsync(studentId);
        }
        
        // Extend service period
        await ExtendServicePeriod(studentId, paymentResult.ServicePeriod);
    }
}
```

### 2. Service Expiration Handling

#### A. Background Service for Expiration Check
```csharp
// Run daily to check for expired services
public async Task CheckExpiredServicesAsync()
{
    var expiredStudents = await GetStudentsWithExpiredServices();
    
    foreach (var student in expiredStudents)
    {
        if (student.Status == StudentStatus.Active)
        {
            await _studentService.DeactivateStudentAsync(
                student.Id, 
                "Service expired - payment required"
            );
        }
    }
}
```

#### B. Grace Period Logic
```csharp
// Optional: Add grace period before deactivation
public async Task HandleServiceExpirationAsync(Guid studentId)
{
    var student = await _studentService.GetStudentByIdAsync(studentId);
    var serviceEndDate = await GetServiceEndDate(studentId);
    var gracePeriodDays = 7; // Configurable
    
    if (DateTime.UtcNow > serviceEndDate.AddDays(gracePeriodDays))
    {
        await _studentService.DeactivateStudentAsync(
            studentId, 
            $"Service expired on {serviceEndDate:yyyy-MM-dd}"
        );
    }
}
```

## 📊 Database Schema Considerations

### TransportFeeItem Table Usage:
```sql
-- Track payment history and service periods
CREATE TABLE TransportFeeItems (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    StudentId UNIQUEIDENTIFIER NOT NULL,
    Amount DECIMAL(19,4) NOT NULL,
    Status NVARCHAR(50) NOT NULL, -- 'Paid', 'Pending', 'Expired'
    Content NVARCHAR(1000),
    ServiceStartDate DATETIME2(3),
    ServiceEndDate DATETIME2(3),
    TransactionId UNIQUEIDENTIFIER,
    CreatedAt DATETIME2(3) DEFAULT GETUTCDATE(),
    IsDeleted BIT DEFAULT 0
);
```

## 🔄 API Endpoints for Payment Team

### 1. Activate Student by Payment
```http
POST /api/Student/{id}/activate-by-payment
Authorization: Bearer {payment-service-token}
Content-Type: application/json

{
    "paymentTransactionId": "TXN123456",
    "amount": 500000,
    "servicePeriod": "2024-01-01 to 2024-12-31"
}
```

### 2. Check Student Payment Status
```http
GET /api/Student/{id}/payment-status
Authorization: Bearer {payment-service-token}
```

### 3. Get Students for Payment Processing
```http
GET /api/Student/status/2?includePaymentInfo=true
Authorization: Bearer {payment-service-token}
```

## 🚀 Implementation Steps for Payment Team

### Phase 1: Basic Integration
1. **Call existing method**: `ActivateStudentByPaymentAsync(studentId)`
2. **Create transport fee records**: Track payment history
3. **Test with existing students**: Verify status changes

### Phase 2: Advanced Features
1. **Service period tracking**: Add start/end dates
2. **Expiration handling**: Background service for auto-deactivation
3. **Grace period logic**: Allow buffer time before deactivation

### Phase 3: Business Intelligence
1. **Payment analytics**: Track payment patterns
2. **Churn prediction**: Identify students likely to not renew
3. **Automated reminders**: Notify parents before expiration

## ⚠️ Important Notes

### Security Considerations:
- Payment service should have dedicated API tokens
- Validate payment amounts and transaction IDs
- Log all payment-related status changes

### Error Handling:
- Handle payment failures gracefully
- Maintain status consistency
- Provide clear error messages

### Performance:
- Use background services for expiration checks
- Implement caching for frequently accessed data
- Consider database indexing for payment queries

## 📝 Testing Scenarios

### 1. First-time Payment
- Student in `Available` status
- Payment successful → Status becomes `Active`
- Transport fee record created

### 2. Renewal Payment
- Student in `Active` status
- Payment successful → Status remains `Active`
- Service period extended

### 3. Expired Service
- Student in `Active` status
- Service expires → Status becomes `Inactive`
- Grace period applied

### 4. Payment Failure
- Student status unchanged
- Error logged
- Retry mechanism available

## 🎯 Success Metrics

- **Payment Success Rate**: >95%
- **Status Update Accuracy**: 100%
- **Service Uptime**: >99.9%
- **Payment Processing Time**: <5 seconds

---

**Contact**: For questions about this specification, contact the Student Status Management team.
**Last Updated**: 2024-01-XX
**Version**: 1.0
