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

### 2.1 Get Users List

**Endpoint:** `GET /api/UserAccount`
**Authorization:** Bearer Token (Admin only)

**Query Parameters:**

- `status` (optional): "active" or "inactive"
- `page` (optional): Page number (default: 1)
- `perPage` (optional): Items per page (default: 20, max: 100)
- `sortBy` (optional): Field to sort by (e.g., "firstName", "email", "createdAt")
- `sortOrder` (optional): "asc" or "desc" (default: "desc")

**Example Requests:**

```bash
# Get all active users, sorted by first name
GET /api/UserAccount?status=active&sortBy=firstName&sortOrder=asc

# Get users with pagination
GET /api/UserAccount?page=2&perPage=5

# Get inactive users
GET /api/UserAccount?status=inactive
```

**Expected Response:**

```json
{
  "users": [
    {
      "id": "12345678-1234-1234-1234-123456789012",
      "email": "john.doe@example.com",
      "firstName": "John",
      "lastName": "Doe",
      "phoneNumber": "+1234567890",
      "address": "123 Main St, City, State",
      "dateOfBirth": "1990-01-01T00:00:00",
      "gender": 1,
      "userPhotoFileId": "87654321-4321-4321-4321-210987654321",
      "createdAt": "2024-01-15T10:30:00Z",
      "updatedAt": "2024-01-15T10:30:00Z",
      "isDeleted": false
    }
  ],
  "totalCount": 50,
  "page": 1,
  "perPage": 10,
  "totalPages": 5
}
```

### 2.2 Get User by ID

**Endpoint:** `GET /api/UserAccount/{userId}`
**Authorization:** Bearer Token (All authenticated users)

**Description:** Get detailed information of a specific user. Users can view their own information, while administrators can view any user's information.

**Example Request:**

```bash
GET /api/UserAccount/12345678-1234-1234-1234-123456789012
```

**Expected Response:**

```json
{
  "id": "12345678-1234-1234-1234-123456789012",
  "email": "john.doe@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "phoneNumber": "+1234567890",
  "address": "123 Main St, City, State",
  "dateOfBirth": "1990-01-01T00:00:00",
  "gender": 1,
  "userPhotoFileId": "87654321-4321-4321-4321-210987654321",
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-01-15T10:30:00Z",
  "isDeleted": false
}
```

**Error Responses:**

- `403`: Forbidden - User trying to access another user's data
- `404`: User not found

### 2.3 Update User (Full Update)

**Endpoint:** `PUT /api/UserAccount/{userId}`
**Authorization:** Bearer Token (All authenticated users)

**Description:** Perform a complete update of user information. Users can update their own information, while administrators can update any user's information.

**Request Body:**

```json
{
  "email": "john.doe@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "phoneNumber": "+1234567890",
  "gender": 1,
  "dateOfBirth": "1990-01-01",
  "address": "123 Main St, City, State"
}
```

**Expected Response:** Returns updated user object

**Error Responses:**

- `400`: Validation errors or business rule violations
- `403`: Forbidden - User trying to update another user's data
- `404`: User not found

### 2.4 Update User (Partial Update)

**Endpoint:** `PATCH /api/UserAccount/{userId}`
**Authorization:** Bearer Token (All authenticated users)

**Description:** Perform a partial update of user information. Users can update their own information, while administrators can update any user's information.

**Request Body:** All fields are optional

```json
{
  "firstName": "Jane",
  "email": "jane.doe@example.com"
}
```

**Expected Response:** Returns updated user object

**Error Responses:**

- `400`: Validation errors or business rule violations
- `403`: Forbidden - User trying to update another user's data
- `404`: User not found

### 2.5 Delete User

**Endpoint:** `DELETE /api/UserAccount/{userId}`
**Authorization:** Bearer Token (Admin only)

**Expected Response:**

```json
{
  "success": true,
  "data": {
    "message": "User deleted successfully"
  },
  "error": null
}
```

**Note:** This performs a soft delete (sets IsDeleted = true)

### 2.6 Upload User Photo

**Endpoint:** `POST /api/UserAccount/{userId}/upload-user-photo`
**Authorization:** Bearer Token (All authenticated users)
**Content-Type:** multipart/form-data

**Description:** Upload a profile photo for a user. Users can upload their own photo, while administrators can upload photos for any user.

**Form Data:**

- `file`: [Select image file - JPG, PNG, max 2MB]

**Expected Response:**

```json
{
  "fileId": "12345678-1234-1234-1234-123456789012",
  "message": "User photo uploaded successfully."
}
```

**Error Responses:**

- `400`: No file provided or invalid file
- `403`: Forbidden - User trying to upload photo for another user
- `500`: Server error

### 2.7 Get User Photo

**Endpoint:** `GET /api/UserAccount/{userId}/user-photo`
**Authorization:** Bearer Token (All authenticated users)

**Description:** Retrieve a user's profile photo. Users can view their own photo, while administrators can view any user's photo.

**Expected Response:** Image file

**Error Responses:**

- `403`: Forbidden - User trying to view another user's photo
- `404`: User photo not found
- `500`: Server error

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
**Authorization:** Bearer Token (All authenticated users)
**Content-Type:** multipart/form-data

**Description:** Upload a health certificate for a driver. Drivers can upload their own certificate, while administrators can upload certificates for any driver.

**Form Data:**

- `file`: [Select file - PDF, JPG, PNG, max 5MB]

**Expected Response:**

```json
{
  "fileId": "12345678-1234-1234-1234-123456789012",
  "message": "Health certificate uploaded successfully."
}
```

**Error Responses:**

- `400`: No file provided or invalid file
- `403`: Forbidden - Driver trying to upload certificate for another driver
- `500`: Server error

### 3.5 Get Health Certificate

**Endpoint:** `GET /api/Driver/{driverId}/health-certificate`
**Authorization:** Bearer Token (All authenticated users)

**Description:** Retrieve a driver's health certificate. Drivers can view their own certificate, while administrators can view any driver's certificate.

**Expected Response:** File download

**Error Responses:**

- `403`: Forbidden - Driver trying to view another driver's certificate
- `404`: Health certificate not found
- `500`: Server error

---

## 4. Driver License Management

### 4.1 Create Driver License

**Endpoint:** `POST /api/DriverLicense`
**Authorization:** Bearer Token (Admin only)

**Description:** Create a new driver license. Only administrators can create driver licenses.

**Request Body:**

```json
{
  "driverId": "12345678-1234-1234-1234-123456789012",
  "licenseNumber": "DL123456789",
  "dateOfIssue": "2023-01-15",
  "issuedBy": "Department of Motor Vehicles"
}
```

**Expected Response:**

```json
{
  "id": "87654321-4321-4321-4321-210987654321",
  "driverId": "12345678-1234-1234-1234-123456789012",
  "dateOfIssue": "2023-01-15T00:00:00",
  "issuedBy": "Department of Motor Vehicles",
  "licenseImageFileId": null,
  "createdBy": "12345678-1234-1234-1234-123456789012",
  "updatedBy": null,
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": null
}
```

### 4.2 Get Driver License by Driver ID

**Endpoint:** `GET /api/DriverLicense/driver/{driverId}`
**Authorization:** Bearer Token (All authenticated users)

**Description:** Get driver license information. Drivers can view their own license, while administrators can view any driver's license.

**Expected Response:**

```json
{
  "id": "87654321-4321-4321-4321-210987654321",
  "driverId": "12345678-1234-1234-1234-123456789012",
  "dateOfIssue": "2023-01-15T00:00:00",
  "issuedBy": "Department of Motor Vehicles",
  "licenseImageFileId": "11111111-1111-1111-1111-111111111111",
  "createdBy": "12345678-1234-1234-1234-123456789012",
  "updatedBy": null,
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": null
}
```

**Error Responses:**

- `403`: Forbidden - Driver trying to view another driver's license
- `404`: Driver license not found

### 4.3 Update Driver License

**Endpoint:** `PUT /api/DriverLicense/{id}`
**Authorization:** Bearer Token (All authenticated users)

**Description:** Update driver license information. Drivers can update their own license, while administrators can update any driver's license.

**Request Body:**

```json
{
  "driverId": "12345678-1234-1234-1234-123456789012",
  "licenseNumber": "DL987654321",
  "dateOfIssue": "2023-06-15",
  "issuedBy": "Department of Motor Vehicles"
}
```

**Expected Response:** Returns updated driver license object

**Error Responses:**

- `400`: Validation errors or business rule violations
- `403`: Forbidden - Driver trying to update another driver's license
- `404`: Driver license not found

### 4.4 Delete Driver License

**Endpoint:** `DELETE /api/DriverLicense/{id}`
**Authorization:** Bearer Token (Admin only)

**Description:** Delete a driver license. Only administrators can delete driver licenses.

**Expected Response:** 204 No Content

**Error Responses:**

- `404`: Driver license not found

### 4.5 Upload License Image

**Endpoint:** `POST /api/DriverLicense/license-image/{driverLicenseId}`
**Authorization:** Bearer Token (All authenticated users)
**Content-Type:** multipart/form-data

**Description:** Upload a license image for a driver license. Drivers can upload their own license image, while administrators can upload license images for any driver.

**Form Data:**

- `file`: [Select file - JPG, PNG, PDF, max 5MB]

**Expected Response:**

```json
{
  "fileId": "11111111-1111-1111-1111-111111111111",
  "message": "License image uploaded successfully."
}
```

**Error Responses:**

- `400`: No file provided or invalid file
- `403`: Forbidden - Driver trying to upload license image for another driver
- `404`: Driver license not found
- `500`: Server error

---

## 5. Parent Management

### 5.1 Create Parent

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

### 5.2 Import Parents from Excel

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

### 5.3 Export Parents to Excel

**Endpoint:** `GET /api/Parent/export`
**Authorization:** Bearer Token (Admin only)

**Expected Response:** Excel file download

---

## 6. File Management

### 6.1 Upload User Photo (via FileController)

**Endpoint:** `POST /api/File/user-photo/{userId}`
**Authorization:** Bearer Token (All roles)
**Content-Type:** multipart/form-data

**Form Data:**

- `file`: [Select image file - JPG, PNG, max 2MB]

### 6.2 Upload Health Certificate (via FileController)

**Endpoint:** `POST /api/File/health-certificate/{driverId}`
**Authorization:** Bearer Token (Admin, Driver)
**Content-Type:** multipart/form-data

**Form Data:**

- `file`: [Select file - PDF, JPG, PNG, max 5MB]

### 6.3 Upload License Image (via FileController)

**Endpoint:** `POST /api/File/license-image/{driverLicenseId}`
**Authorization:** Bearer Token (Admin, Driver)
**Content-Type:** multipart/form-data

**Form Data:**

- `file`: [Select file - JPG, PNG, PDF, max 5MB]

### 6.4 Download File

**Endpoint:** `GET /api/File/{fileId}`
**Authorization:** Bearer Token (All roles)

**Expected Response:** File download

### 6.5 Delete File

**Endpoint:** `DELETE /api/File/{fileId}`
**Authorization:** Bearer Token (Admin only)

**Expected Response:** 204 No Content

---

## 7. Test Data Sets

### 7.1 Admin Users

```json
{
  "email": "admin@edubus.com",
  "password": "Admin123!"
}
```

### 7.2 Driver Users

```json
{
  "email": "driver1@edubus.com",
  "password": "Driver123!"
}
```

### 7.3 Parent Users

```json
{
  "email": "parent1@edubus.com",
  "password": "Parent123!"
}
```

---

## 8. Test Scenarios

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
