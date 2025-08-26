# API Test Scenarios for Swagger

## 1. Authentication APIs

### 1.1 Login

**Endpoint:** `POST /api/Auth/login`

**Request Body:**

```json
{
  "email": "admin@edubus.com",
  "password": "password"
}
```

**Expected Response:**

```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "abc123def456...",
    "fullName": "Admin User",
    "role": "Admin",
    "expiresAtUtc": "2024-01-15T10:30:00Z"
  },
  "error": null
}
```

### 1.2 Refresh Token

**Endpoint:** `POST /api/Auth/refresh-token`

**Request Body:**

```json
{
  "refreshToken": "abc123def456..."
}
```

### 1.3 Logout

**Endpoint:** `POST /api/Auth/logout`
**Authorization:** Bearer Token required

### 1.4 Test Role-based Access

**Endpoints:**

- `GET /api/Auth/admin` - Admin only
- `GET /api/Auth/driver` - Driver only
- `GET /api/Auth/parent` - Parent only
- `GET /api/Auth/any` - Any authenticated user

---

## 2. User Account Management

### 2.1 Upload User Photo

**Endpoint:** `POST /api/UserAccount/{userId}/upload-user-photo`
**Authorization:** Bearer Token (All roles)
**Content-Type:** multipart/form-data

**Form Data:**

- `file`: [Select image file - JPG, PNG, max 2MB]

**Expected Response:**

```json
{
  "fileId": "12345678-1234-1234-1234-123456789012",
  "message": "User photo uploaded successfully."
}
```

### 2.2 Get User Photo

**Endpoint:** `GET /api/UserAccount/{userId}/user-photo`
**Authorization:** Bearer Token (All roles)

**Expected Response:** Image file

---

## 3. Driver Management

### 3.1 Create Driver

**Endpoint:** `POST /api/Driver`
**Authorization:** Bearer Token (Admin only)

**Request Body:**

```json
{
  "email": "driver1@edubus.com",
  "firstName": "John",
  "lastName": "Driver",
  "phoneNumber": "0123456789",
  "gender": 1,
  "dateOfBirth": "1990-01-15",
  "address": "123 Driver Street, City"
}
```

**Expected Response:**

```json
{
  "id": "12345678-1234-1234-1234-123456789012",
  "email": "driver1@edubus.com",
  "firstName": "John",
  "lastName": "Driver",
  "phoneNumber": "0123456789",
  "gender": 1,
  "dateOfBirth": "1990-01-15T00:00:00",
  "address": "123 Driver Street, City",
  "password": "GeneratedPassword123!"
}
```

### 3.2 Import Drivers from Excel

**Endpoint:** `POST /api/Driver/import`
**Authorization:** Bearer Token (Admin only)
**Content-Type:** multipart/form-data

**Form Data:**

- `file`: [Select Excel file - .xlsx format]

**Excel Format:**
| First Name | Last Name | Email | Phone | Gender | Date of Birth | Address |
|------------|-----------|-------|-------|--------|---------------|---------|
| John | Driver | john.driver@edubus.com | 0123456789 | 1 | 15/01/1990 | 123 Driver St |
| | Driver | jane.driver@edubus.com | 0987654321 | 2 | 20/05/1985 | 456 Driver Ave |

**Expected Response:**

```json
{
  "totalProcessed": 2,
  "successUsers": [
    {
      "rowNumber": 1,
      "email": "john.driver@edubus.com",
      "firstName": "John",
      "lastName": "Driver",
      "password": "GeneratedPassword123!"
    }
  ],
  "failedUsers": []
}
```

### 3.3 Export Drivers to Excel

**Endpoint:** `GET /api/Driver/export`
**Authorization:** Bearer Token (Admin only)

**Expected Response:** Excel file download

### 3.4 Upload Health Certificate

**Endpoint:** `POST /api/Driver/{driverId}/upload-health-certificate`
**Authorization:** Bearer Token (Admin, Driver)
**Content-Type:** multipart/form-data

**Form Data:**

- `file`: [Select file - PDF, JPG, PNG, max 5MB]

**Expected Response:**

```json
{
  "fileId": "12345678-1234-1234-1234-123456789012",
  "message": "Health certificate uploaded successfully."
}
```

### 3.5 Get Health Certificate

**Endpoint:** `GET /api/Driver/{driverId}/health-certificate`
**Authorization:** Bearer Token (Admin, Driver)

**Expected Response:** File download

---

## 4. Parent Management

### 4.1 Create Parent

**Endpoint:** `POST /api/Parent`
**Authorization:** Bearer Token (Admin only)

**Request Body:**

```json
{
  "email": "parent1@edubus.com",
  "firstName": "Mary",
  "lastName": "Parent",
  "phoneNumber": "0123456789",
  "gender": 2,
  "dateOfBirth": "1985-03-20",
  "address": "456 Parent Avenue, City"
}
```

**Expected Response:**

```json
{
  "id": "12345678-1234-1234-1234-123456789012",
  "email": "parent1@edubus.com",
  "firstName": "Mary",
  "lastName": "Parent",
  "phoneNumber": "0123456789",
  "gender": 2,
  "dateOfBirth": "1985-03-20T00:00:00",
  "address": "456 Parent Avenue, City",
  "password": "GeneratedPassword123!"
}
```

### 4.2 Import Parents from Excel

**Endpoint:** `POST /api/Parent/import`
**Authorization:** Bearer Token (Admin only)
**Content-Type:** multipart/form-data

**Form Data:**

- `file`: [Select Excel file - .xlsx format]

**Excel Format:**
| First Name | Last Name | Email | Phone | Gender | Date of Birth | Address |
|------------|-----------|-------|-------|--------|---------------|---------|
| Mary | Parent | mary.parent@edubus.com | 0123456789 | 2 | 20/03/1985 | 456 Parent Ave |
| Tom | Parent | tom.parent@edubus.com | 0987654321 | 1 | 10/07/1988 | 789 Parent St |

### 4.3 Export Parents to Excel

**Endpoint:** `GET /api/Parent/export`
**Authorization:** Bearer Token (Admin only)

**Expected Response:** Excel file download

---

## 5. File Management

### 5.1 Upload User Photo (via FileController)

**Endpoint:** `POST /api/File/user-photo/{userId}`
**Authorization:** Bearer Token (All roles)
**Content-Type:** multipart/form-data

**Form Data:**

- `file`: [Select image file - JPG, PNG, max 2MB]

### 5.2 Upload Health Certificate (via FileController)

**Endpoint:** `POST /api/File/health-certificate/{driverId}`
**Authorization:** Bearer Token (Admin, Driver)
**Content-Type:** multipart/form-data

**Form Data:**

- `file`: [Select file - PDF, JPG, PNG, max 5MB]

### 5.3 Upload License Image (via FileController)

**Endpoint:** `POST /api/File/license-image/{driverLicenseId}`
**Authorization:** Bearer Token (Admin, Driver)
**Content-Type:** multipart/form-data

**Form Data:**

- `file`: [Select file - JPG, PNG, PDF, max 5MB]

### 5.4 Download File

**Endpoint:** `GET /api/File/{fileId}`
**Authorization:** Bearer Token (All roles)

**Expected Response:** File download

### 5.5 Delete File

**Endpoint:** `DELETE /api/File/{fileId}`
**Authorization:** Bearer Token (Admin only)

**Expected Response:** 204 No Content

---

## 6. Test Data Sets

### 6.1 Admin Users

```json
{
  "email": "admin@edubus.com",
  "password": "Admin123!"
}
```

### 6.2 Driver Users

```json
{
  "email": "driver1@edubus.com",
  "password": "Driver123!"
}
```

### 6.3 Parent Users

```json
{
  "email": "parent1@edubus.com",
  "password": "Parent123!"
}
```

---

## 7. Test Scenarios

### Scenario 1: Complete Driver Registration Flow

1. Login as Admin
2. Create Driver
3. Upload Driver's User Photo
4. Upload Driver's Health Certificate
5. Download Driver's Health Certificate

### Scenario 2: Complete Parent Registration Flow

1. Login as Admin
2. Create Parent
3. Upload Parent's User Photo

### Scenario 3: File Management Flow

1. Login as any user
2. Upload various file types
3. Download files
4. Delete files (Admin only)

### Scenario 4: Import/Export Flow

1. Login as Admin
2. Export current data to Excel
3. Import data from Excel
4. Verify imported data

### Scenario 5: Authorization Testing

1. Test each endpoint with different user roles
2. Verify access restrictions work correctly
3. Test unauthorized access attempts

---

## 8. Error Scenarios to Test

### 8.1 Authentication Errors

- Invalid credentials
- Expired token
- Missing token
- Invalid refresh token

### 8.2 Authorization Errors

- Accessing Admin-only endpoints as Driver/Parent
- Accessing Driver-only endpoints as Parent
- Accessing Parent-only endpoints as Driver

### 8.3 File Upload Errors

- File too large
- Invalid file type
- Missing file
- Corrupted file

### 8.4 Data Validation Errors

- Invalid email format
- Duplicate email
- Invalid phone number
- Missing required fields
- Invalid date format

### 8.5 Business Logic Errors

- Uploading health certificate for non-driver user
- Accessing non-existent files
- Importing duplicate data
