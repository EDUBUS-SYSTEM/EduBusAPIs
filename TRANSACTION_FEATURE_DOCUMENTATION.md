# 🚌 EduBus Transaction Management Feature Documentation

## 📋 Overview
This document describes the complete transaction management feature for EduBus system, including unit price management, pickup point request submission, admin approval, and automatic transaction creation.

## 🎯 Feature Flow

### 1. **Unit Price Management (Admin)**
**Purpose**: Admin creates and manages transport fee pricing per kilometer.

**API Endpoints**:
- `POST /api/UnitPrice` - Create new unit price
- `GET /api/UnitPrice` - Get list of unit prices
- `PUT /api/UnitPrice/{id}` - Update unit price
- `DELETE /api/UnitPrice/{id}` - Delete unit price

**Key Features**:
- Only one active unit price can exist at a time
- Automatic validation for overlapping date ranges
- Price per kilometer covers round trip (2 trips per day)

**Data Model**:
```csharp
public class UnitPrice
{
    public Guid Id { get; set; }
    public decimal PricePerKm { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

---

### 2. **Fee Calculation API**
**Purpose**: Calculate transport fee for parents before submitting pickup point request.

**API Endpoint**: `POST /api/Transaction/calculate-fee`

**Request**:
```json
{
    "distanceKm": 5.5,
    "unitPriceId": "optional-guid"
}
```

**Response**:
```json
{
    "totalFee": 495000,
    "unitPricePerKm": 5000,
    "distanceKm": 5.5,
    "totalSchoolDays": 99,
    "totalTrips": 198,
    "totalDistanceKm": 544.5,
    "semesterName": "Semester 2",
    "academicYear": "2025-2026",
    "semesterStartDate": "2026-01-04T17:00:00Z",
    "semesterEndDate": "2026-05-30T17:00:00Z",
    "holidays": ["2026-02-08T00:00:00Z", "..."],
    "calculationDetails": "Transport fee for Semester 2 2025-2026:\n- Distance: 5.5 km\n- Unit price: 5,000 VND/km\n- School days: 99 days (excluding weekends and holidays)\n- Total trips: 198 trips (round trip per day)\n- Total distance: 544.5 km\n- Total fee: 495,000 VND"
}
```

**Calculation Logic**:
1. Get next upcoming semester (not current active semester)
2. Fetch active unit price per kilometer
3. Calculate school days excluding weekends and holidays using RRule from Schedule
4. Total distance = Distance × School Days
5. Total fee = Unit Price × Total Distance

---

### 3. **Next Semester API**
**Purpose**: Get information about the next upcoming semester for parents.

**API Endpoint**: `GET /api/Transaction/next-semester`

**Response**:
```json
{
    "name": "Semester 2",
    "code": "S2",
    "academicYear": "2025-2026",
    "startDate": "2026-01-04T17:00:00Z",
    "endDate": "2026-05-30T17:00:00Z",
    "holidays": ["2026-02-08T00:00:00Z", "..."],
    "totalSchoolDays": 99,
    "totalTrips": 198
}
```

---

### 4. **Pickup Point Request Submission (Parent)**
**Purpose**: Parents submit pickup point requests with calculated fee information.

**Enhancements from Previous Version**:
- **New Fields Added**:
  - `NextSemesterName`: Name of upcoming semester
  - `NextSemesterCode`: Code of upcoming semester  
  - `NextSemesterStartDate`: Start date of upcoming semester
  - `NextSemesterEndDate`: End date of upcoming semester
  - `NextSemesterAcademicYear`: Academic year of upcoming semester
  - `TotalSchoolDays`: Number of school days in semester
  - `TotalTrips`: Total trips (school days × 2)
  - `TotalDistanceKm`: Total distance for semester
  - `UnitPricePerKm`: Unit price used for calculation
  - `CalculationDetails`: Detailed calculation breakdown
  - `TotalFee`: Pre-calculated total fee per student

**API Endpoint**: `POST /api/PickupPoint/submit-request`

**Request**:
```json
{
    "parentEmail": "parent@example.com",
    "parentPhone": "0123456789",
    "parentName": "John Doe",
    "students": [
        {
            "studentId": "student-guid-1",
            "studentName": "Student 1"
        }
    ],
    "location": "155 Nguyen Van Thoai",
    "latitude": 16.0738,
    "longitude": 108.2597,
    "distanceKm": 5.5,
    "totalFee": 495000,
    "nextSemesterName": "Semester 2",
    "nextSemesterCode": "S2",
    "nextSemesterStartDate": "2026-01-04T17:00:00Z",
    "nextSemesterEndDate": "2026-05-30T17:00:00Z",
    "nextSemesterAcademicYear": "2025-2026",
    "totalSchoolDays": 99,
    "totalTrips": 198,
    "totalDistanceKm": 544.5,
    "unitPricePerKm": 5000,
    "calculationDetails": "Transport fee for Semester 2 2025-2026: ..."
}
```

**Response**:
```json
{
    "requestId": "request-guid",
    "message": "Pickup point request submitted successfully",
    "semesterInfo": {
        "name": "Semester 2",
        "code": "S2",
        "academicYear": "2025-2026",
        "startDate": "2026-01-04T17:00:00Z",
        "endDate": "2026-05-30T17:00:00Z",
        "totalSchoolDays": 99,
        "totalTrips": 198
    },
    "feeCalculation": {
        "distanceKm": 5.5,
        "unitPricePerKm": 5000,
        "totalSchoolDays": 99,
        "totalDistanceKm": 544.5,
        "totalFee": 495000,
        "calculationDetails": "Transport fee for Semester 2 2025-2026: ..."
    }
}
```

---

### 5. **Admin Approval Process**
**Purpose**: Admin reviews and approves pickup point requests, triggering automatic transaction creation.

**API Endpoint**: `PUT /api/PickupPoint/approve-request/{requestId}`

**What Happens During Approval**:

1. **Student Status Update**: 
   - Updates all students in request to `StudentStatus.Pending`

2. **Parent Account Creation**:
   - If parent email doesn't exist, creates new parent account
   - Generates random password and sends via email

3. **Email Notification**:
   - Sends approval email to parent with account credentials
   - Includes pickup point details and payment instructions

4. **Pickup Point Creation**:
   - Creates `PickupPoint` entity with location details
   - Stores pickup point ID in request document

5. **Transaction Creation** (NEW FEATURE):
   - Creates **separate transaction for each student**
   - Each transaction includes one `TransportFeeItem`
   - Uses pre-calculated fee from request submission

**Transaction Creation Details**:
```csharp
// For each student in the request:
var transaction = new Transaction
{
    ParentId = parentId,
    TransactionCode = GenerateTransactionCode(),
    Status = TransactionStatus.Pending,
    Amount = request.TotalFee, // Pre-calculated per student
    Currency = "VND",
    Description = $"Transport fee for student {studentName}",
    Provider = "PayOS"
};

var transportFeeItem = new TransportFeeItem
{
    StudentId = studentId,
    TransactionId = transaction.Id,
    Description = $"Transport fee for student {studentName}",
    DistanceKm = request.DistanceKm,
    UnitPriceVndPerKm = request.UnitPricePerKm,
    Subtotal = request.TotalFee, // Per student amount
    SemesterCode = request.NextSemesterCode,
    AcademicYear = request.NextSemesterAcademicYear,
    Type = TransportFeeItemType.Register,
    Status = TransportFeeItemStatus.Unbilled,
    UnitPriceId = activeUnitPriceId
};
```

---

### 6. **Payment Integration**
**Purpose**: Handle payment processing and update transaction status.

**Payment Success Flow**:
1. PayOS webhook notifies payment success
2. `PaymentService.ActivateStudentsForTransactionAsync()` is called
3. **Pickup Point Assignment** (NEW FEATURE):
   - Assigns pickup point to students
   - Updates `Student.CurrentPickupPointId`
   - Sets `Student.PickupPointAssignedAt`
   - Creates `StudentPickupPointHistory` record
   - Updates student status to `StudentStatus.Active`
4. Updates `TransportFeeItem.Status` to `TransportFeeItemStatus.Paid`

---

### 7. **Transaction Management APIs**
**Purpose**: Full CRUD operations for transactions.

**API Endpoints**:
- `GET /api/Transaction` - Get transaction list with pagination and filters
- `GET /api/Transaction/{id}` - Get transaction details
- `GET /api/Transaction/by-student/{studentId}` - Get transactions by student
- `GET /api/Transaction/by-transport-fee-item/{transportFeeItemId}` - Get transaction by transport fee item
- `PUT /api/Transaction/{id}` - Update transaction
- `DELETE /api/Transaction/{id}` - Soft delete transaction

**Transaction List Request**:
```json
{
    "page": 1,
    "pageSize": 10,
    "parentId": "optional-parent-guid",
    "studentId": "optional-student-guid",
    "status": "Pending|Paid|Failed",
    "transactionCode": "optional-code",
    "startDate": "2025-01-01",
    "endDate": "2025-12-31"
}
```

**Transaction Detail Response**:
```json
{
    "id": "transaction-guid",
    "parentId": "parent-guid",
    "parentEmail": "parent@example.com",
    "transactionCode": "TXN-20250101-001",
    "status": "Pending",
    "amount": 495000,
    "currency": "VND",
    "description": "Transport fee for student John Doe",
    "provider": "PayOS",
    "createdAt": "2025-01-01T10:00:00Z",
    "paidAt": null,
    "transportFeeItems": [
        {
            "id": "transport-fee-item-guid",
            "studentId": "student-guid",
            "studentName": "John Doe",
            "description": "Transport fee for student John Doe",
            "distanceKm": 5.5,
            "unitPricePerKm": 5000,
            "amount": 495000,
            "semesterName": "Semester 2",
            "academicYear": "2025-2026",
            "type": "Register",
            "status": "Unbilled"
        }
    ]
}
```

---

### 8. **Pickup Points with Student Status API**
**Purpose**: Get pickup points that are currently assigned to active students.

**API Endpoint**: `GET /api/PickupPoint/with-student-status`

**Response**:
```json
[
    {
        "id": "pickup-point-guid",
        "description": "Register shuttle for 2 students",
        "location": "155 Nguyen Van Thoai",
        "latitude": 16.0738,
        "longitude": 108.2597,
        "createdAt": "2025-09-27T12:25:00.768Z",
        "updatedAt": null,
        "isDeleted": false,
        "assignedStudentCount": 1,
        "assignedStudents": [
            {
                "id": "student-guid",
                "firstName": "Nguyen Thi",
                "lastName": "Thu Nguyet",
                "fullName": "Nguyen Thi Thu Nguyet",
                "status": "Active",
                "pickupPointAssignedAt": "2025-09-27T12:28:04.233Z"
            }
        ]
    }
]
```

**Key Features**:
- Only returns pickup points assigned to **Active** students
- Excludes pickup points with no assigned students
- Includes detailed student information

---

## 🧪 Testing Guide

### 1. **Unit Price Management Testing**

**Test Case 1: Create Unit Price**
```bash
POST /api/UnitPrice
{
    "pricePerKm": 5000,
    "startDate": "2025-01-01",
    "endDate": "2025-12-31"
}
```
**Expected**: Unit price created successfully, becomes active

**Test Case 2: Overlapping Date Range**
```bash
POST /api/UnitPrice
{
    "pricePerKm": 6000,
    "startDate": "2025-06-01",
    "endDate": "2026-06-01"
}
```
**Expected**: Validation error - overlapping date range

### 2. **Fee Calculation Testing**

**Test Case 3: Calculate Fee**
```bash
POST /api/Transaction/calculate-fee
{
    "distanceKm": 5.5
}
```
**Expected**: Returns detailed calculation with correct total fee

**Test Case 4: Get Next Semester**
```bash
GET /api/Transaction/next-semester
```
**Expected**: Returns upcoming semester info with holidays and school days

### 3. **Pickup Point Request Testing**

**Test Case 5: Submit Request**
```bash
POST /api/PickupPoint/submit-request
{
    "parentEmail": "test@example.com",
    "parentPhone": "0123456789",
    "parentName": "Test Parent",
    "students": [{"studentId": "student-guid", "studentName": "Test Student"}],
    "location": "Test Location",
    "latitude": 16.0738,
    "longitude": 108.2597,
    "distanceKm": 5.5,
    "totalFee": 495000,
    "nextSemesterName": "Semester 2",
    "nextSemesterCode": "S2",
    "nextSemesterStartDate": "2026-01-04T17:00:00Z",
    "nextSemesterEndDate": "2026-05-30T17:00:00Z",
    "nextSemesterAcademicYear": "2025-2026",
    "totalSchoolDays": 99,
    "totalTrips": 198,
    "totalDistanceKm": 544.5,
    "unitPricePerKm": 5000,
    "calculationDetails": "Transport fee calculation details..."
}
```
**Expected**: Request submitted successfully with semester and fee info

### 4. **Admin Approval Testing**

**Test Case 6: Approve Request**
```bash
PUT /api/PickupPoint/approve-request/{requestId}
```
**Expected**: 
- Student status updated to Pending
- Parent account created (if new)
- Approval email sent
- Pickup point created
- Transaction and TransportFeeItem created for each student

**Verify Database**:
```sql
-- Check transactions created
SELECT * FROM Transactions WHERE Description LIKE '%Transport fee%';

-- Check transport fee items
SELECT * FROM TransportFeeItems WHERE Type = 'Register';

-- Check student status
SELECT * FROM Students WHERE Status = 'Pending';
```

### 5. **Transaction Management Testing**

**Test Case 7: Get Transaction List**
```bash
GET /api/Transaction?page=1&pageSize=10
```
**Expected**: Returns paginated transaction list

**Test Case 8: Get Transaction by Student**
```bash
GET /api/Transaction/by-student/{studentId}
```
**Expected**: Returns transactions for specific student

**Test Case 9: Get Transaction Detail**
```bash
GET /api/Transaction/{transactionId}
```
**Expected**: Returns detailed transaction with transport fee items

### 6. **Payment Integration Testing**

**Test Case 10: Simulate Payment Success**
```bash
POST /api/Payment/payos-webhook
{
    "code": 0,
    "desc": "success",
    "data": {
        "orderCode": "transaction-code",
        "status": "PAID"
    }
}
```
**Expected**: 
- Transaction status updated to Paid
- TransportFeeItem status updated to Paid
- Student assigned to pickup point
- Student status updated to Active

### 7. **Pickup Points with Student Status Testing**

**Test Case 11: Get Assigned Pickup Points**
```bash
GET /api/PickupPoint/with-student-status
```
**Expected**: Returns only pickup points assigned to Active students

---

## 🔍 Review Checklist

### **Code Review Points**:

1. **Data Models**:
   - ✅ `UnitPrice` model has proper validation
   - ✅ `Transaction` model includes all required fields
   - ✅ `TransportFeeItem` model has correct relationships
   - ✅ `PickupPointRequestDocument` stores semester and fee info

2. **Business Logic**:
   - ✅ Fee calculation excludes weekends and holidays
   - ✅ Only one active unit price allowed
   - ✅ Separate transaction per student
   - ✅ Pickup point assignment after payment success

3. **API Design**:
   - ✅ Proper HTTP status codes
   - ✅ Comprehensive error handling
   - ✅ Input validation with data annotations
   - ✅ Pagination for list endpoints

4. **Database Operations**:
   - ✅ Proper use of transactions for data consistency
   - ✅ Soft delete implementation
   - ✅ Foreign key relationships maintained

5. **Security**:
   - ✅ Admin-only endpoints properly protected
   - ✅ Input sanitization
   - ✅ SQL injection prevention

### **Performance Considerations**:

1. **Database Queries**:
   - ✅ Efficient queries with proper indexing
   - ✅ Avoid N+1 query problems
   - ✅ Use projection for list endpoints

2. **Caching**:
   - ✅ Consider caching unit prices
   - ✅ Cache semester information

3. **Scalability**:
   - ✅ Pagination for large datasets
   - ✅ Async operations where appropriate

---

## 🚀 Deployment Notes

1. **Database Migration**: Ensure all new tables and columns are migrated
2. **Environment Variables**: Configure PayOS integration settings
3. **Email Configuration**: Set up SMTP for approval emails
4. **Monitoring**: Add logging for transaction creation and payment processing

---

## 📞 Support

For questions or issues with this feature, please contact the development team or create an issue in the project repository.
