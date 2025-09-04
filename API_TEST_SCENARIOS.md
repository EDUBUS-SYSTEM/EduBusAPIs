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

## =======

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

### 6.1 Upload File (Admin Only)

**Endpoint:** `POST /api/File/upload`
**Authorization:** Bearer Token (Admin only)
**Content-Type:** multipart/form-data

**Form Data:**

- `file`: [Select file]
- `entityType`: "UserAccount" | "Driver" | "DriverLicense" | "Student" | "Parent" | "Template"
- `entityId`: [Guid of entity] (can be empty for Template)
- `fileType`: "UserPhoto" | "HealthCertificate" | "LicenseImage" | "Document" | "Image" | "UserAccount" | "Driver" | "Parent"

**Response:**

```json
{
  "fileId": "guid-here",
  "message": "File uploaded successfully.",
  "entityType": "UserAccount",
  "entityId": "guid-here",
  "fileType": "UserPhoto"
}
```

**Example for Template Upload:**

```bash
curl -X 'POST' \
  'https://localhost:7061/api/File/upload' \
  -H 'Authorization: Bearer {token}' \
  -H 'Content-Type: multipart/form-data' \
  -F 'file=@driver_template.xlsx' \
  -F 'entityType=Template' \
  -F 'entityId=' \
  -F 'fileType=Driver'
```

### 6.2 Download Template File (Admin Only)

**Endpoint:** `GET /api/File/template/{templateType}`
**Authorization:** Bearer Token (Admin only)

**Template Types:**

- `UserAccount` - Template for importing UserAccount
- `Driver` - Template for importing Driver
- `Parent` - Template for importing Parent

**Example:**

```http
GET /api/File/template/UserAccount
Authorization: Bearer {admin_token}
```

**Expected Response:** Excel file download

### 6.3 Download File

**Endpoint:** `GET /api/File/{fileId}`
**Authorization:** Bearer Token (All roles)

**Expected Response:** File download

### 6.4 Delete File

**Endpoint:** `DELETE /api/File/{fileId}`
**Authorization:** Bearer Token (Admin only)

**Expected Response:** 204 No Content

### 6.5 File Type Validation Rules

| File Type         | Allowed Extensions                         | Max Size | Description                 |
| ----------------- | ------------------------------------------ | -------- | --------------------------- |
| UserPhoto         | .jpg, .jpeg, .png                          | 2MB      | User profile photos         |
| HealthCertificate | .pdf, .jpg, .jpeg, .png                    | 5MB      | Driver health certificates  |
| LicenseImage      | .jpg, .jpeg, .png, .pdf                    | 5MB      | Driver license images       |
| Document          | .pdf, .doc, .docx, .txt                    | 10MB     | General documents           |
| Image             | .jpg, .jpeg, .png, .gif, .bmp              | 5MB      | General images              |
| UserAccount       | .xlsx                                      | 10MB     | UserAccount import template |
| Driver            | .xlsx                                      | 10MB     | Driver import template      |
| Parent            | .xlsx                                      | 10MB     | Parent import template      |
| Default           | .pdf, .jpg, .jpeg, .png, .doc, .docx, .txt | 10MB     | Fallback for unknown types  |

**Note:** Specific upload endpoints are already handled in their respective controllers:

- Upload User Photo: `POST /api/UserAccount/{userId}/upload-user-photo`
- Upload Health Certificate: `POST /api/Driver/{driverId}/upload-health-certificate`
- Upload License Image: `POST /api/DriverLicense/license-image/{driverLicenseId}`

**To use Excel templates for import:**

1. **Upload template** using the upload endpoint with:

   - `entityType`: "Template"
   - `entityId`: "00000000-0000-0000-0000-000000000000"
   - `fileType`: "UserAccount" | "Driver" | "Parent"

2. **Download template** using the template endpoint:
   - `GET /api/File/template/UserAccount`
   - `GET /api/File/template/Driver`
   - `GET /api/File/template/Parent`

**Example workflow:**

```bash
# 1. Upload template (Admin only)
POST /api/File/upload
# Response: {"fileId": "12345678-1234-1234-1234-123456789012", ...}

# 2. Download template (Admin only)
GET /api/File/template/UserAccount
# Response: Excel file download
```

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

## 9. Error Scenarios to Test

### 9.1 Authentication Errors

- Invalid credentials
- Expired token
- Missing token
- Invalid refresh token

### 9.2 Authorization Errors

- Accessing Admin-only endpoints as Driver/Parent
- Accessing Driver-only endpoints as Parent
- Accessing Parent-only endpoints as Driver

### 9.3 File Upload Errors

- File too large
- Invalid file type
- Missing file
- Corrupted file

### 9.4 Data Validation Errors

- Invalid email format
- Duplicate email
- Invalid phone number
- Missing required fields
- Invalid date format

### 9.5 Business Logic Errors

- Uploading health certificate for non-driver user
- Accessing non-existent files
- Importing duplicate data

## 9. Vehicle Management

### 9.1 Create Vehicle

Endpoint: POST /api/Vehicle (Admin only)
Request Body:

```json
{
  "licensePlate": "43A-12345",
  "capacity": 16,
  "status": "active",
  "adminId": "550e8400-e29b-41d4-a716-446655440001"
}
```

Expected Response:

```json
{
  "success": true,
  "data": {
    "id": "uuid",
    "licensePlate": "43A-12345",
    "capacity": 16,
    "status": "active",
    "adminId": "550e8400-e29b-41d4-a716-446655440001",
    "createdAt": "2025-08-28T10:00:00Z",
    "isDeleted": false
  },
  "error": null
}
```

### 9.2 Get All Vehicles

Endpoint: GET /api/Vehicle?page=1&perPage=20&sortBy=createdAt&sortOrder=desc (All roles)
Expected Response:

```json
{
  "success": true,
  "data": [
    {
      "id": "uuid",
      "licensePlate": "43A-12345",
      "capacity": 16,
      "status": "active",
      "adminId": "uuid",
      "createdAt": "2025-08-28T10:00:00Z"
    }
  ],
  "error": null
}
```

### 9.3 Get Vehicle by Id

Endpoint: GET /api/Vehicle/{vehicleId} (All roles)
Expected Response: Vehicle details with decrypted licensePlate (if implemented).

### 9.4 Update Vehicle

Endpoint: PUT /api/Vehicle/{vehicleId} (Admin only)
Request Body:

```json
{
  "licensePlate": "43A-67890",
  "capacity": 20,
  "status": "maintenance",
  "adminId": "550e8400-e29b-41d4-a716-446655440001"
}
```

### 9.5 Partial Update Vehicle

Endpoint: PATCH /api/Vehicle/{vehicleId} (Admin only)
Request Body:

```json
{
  "status": "retired"
}
```

### 9.6 Delete Vehicle

Endpoint: DELETE /api/Vehicle/{vehicleId} (Admin only)
Expected Response:

```json
{
  "success": true,
  "data": null,
  "error": null
}
```

### 9.7 Assign Driver to Vehicle

Endpoint: POST /api/Vehicle/{vehicleId}/drivers (Admin only)
Request Body:

```json
{
  "driverId": "550e8400-e29b-41d4-a716-446655440002",
  "isPrimaryDriver": true,
  "startTimeUtc": "2025-08-28T08:00:00Z",
  "endTimeUtc": "2025-12-31T23:59:59Z"
}
```

### 9.8 Get Drivers of Vehicle

Endpoint: GET /api/Vehicle/{vehicleId}/drivers?isActive=true (All roles)
Expected Response:

```json
{
  "success": true,
  "data": [
    {
      "driverId": "uuid",
      "vehicleId": "uuid",
      "isPrimaryDriver": true,
      "startTimeUtc": "2025-08-28T08:00:00Z",
      "endTimeUtc": "2025-12-31T23:59:59Z",
      "driver": {
        "id": "uuid",
        "fullName": "John Driver",
        "phoneNumber": "0123456789"
      }
    }
  ],
  "error": null
}
```

---

## 10. Driver Leave Management

### 10.1 Create Leave Request

**Endpoint:** `POST /api/DriverLeave`
**Authorization:** Bearer Token (Driver only)

**Request Body:**

```json
{
  "driverId": "550e8400-e29b-41d4-a716-446655440002",
  "leaveType": 1,
  "startDate": "2024-01-15T00:00:00Z",
  "endDate": "2024-01-17T00:00:00Z",
  "reason": "Family emergency"
}
```

**Expected Response:**

```json
{
  "success": true,
  "data": {
    "id": "uuid",
    "driverId": "550e8400-e29b-41d4-a716-446655440002",
    "driverName": "John Driver",
    "driverEmail": "john.driver@edubus.com",
    "leaveType": 1,
    "startDate": "2024-01-15T00:00:00Z",
    "endDate": "2024-01-17T00:00:00Z",
    "reason": "Family emergency",
    "status": 0,
    "requestedAt": "2024-01-10T10:30:00Z",
    "totalDays": 3,
    "autoReplacementEnabled": true
  },
  "error": null
}
```

### 10.2 Get Driver Leave Requests

**Endpoint:** `GET /api/DriverLeave/driver/{driverId}?fromDate=2024-01-01&toDate=2024-12-31`
**Authorization:** Bearer Token (Driver or Admin)

**Expected Response:**

```json
{
  "success": true,
  "data": [
    {
      "id": "uuid",
      "driverId": "550e8400-e29b-41d4-a716-446655440002",
      "driverName": "John Driver",
      "leaveType": 1,
      "startDate": "2024-01-15T00:00:00Z",
      "endDate": "2024-01-17T00:00:00Z",
      "status": 0,
      "totalDays": 3
    }
  ],
  "error": null
}
```

### 10.3 Get Pending Leave Requests (Admin)

**Endpoint:** `GET /api/DriverLeave/pending`
**Authorization:** Bearer Token (Admin only)

**Expected Response:**

```json
{
  "success": true,
  "data": [
    {
      "id": "uuid",
      "driverName": "John Driver",
      "leaveType": 1,
      "startDate": "2024-01-15T00:00:00Z",
      "endDate": "2024-01-17T00:00:00Z",
      "reason": "Family emergency",
      "requestedAt": "2024-01-10T10:30:00Z",
      "totalDays": 3
    }
  ],
  "error": null
}
```

### 10.4 Approve Leave Request

**Endpoint:** `PUT /api/DriverLeave/{leaveId}/approve`
**Authorization:** Bearer Token (Admin only)

**Request Body:**

```json
{
  "approvalNote": "Approved for family emergency"
}
```

**Expected Response:**

```json
{
  "success": true,
  "data": {
    "id": "uuid",
    "status": 1,
    "approvedByAdminId": "550e8400-e29b-41d4-a716-446655440001",
    "approvedAt": "2024-01-11T09:00:00Z",
    "approvalNote": "Approved for family emergency"
  },
  "error": null
}
```

### 10.5 Reject Leave Request

**Endpoint:** `PUT /api/DriverLeave/{leaveId}/reject`
**Authorization:** Bearer Token (Admin only)

**Request Body:**

```json
{
  "rejectionReason": "Insufficient notice period"
}
```

**Expected Response:**

```json
{
  "success": true,
  "data": {
    "id": "uuid",
    "status": 2,
    "approvedByAdminId": "550e8400-e29b-41d4-a716-446655440001",
    "approvedAt": "2024-01-11T09:00:00Z",
    "approvalNote": "Insufficient notice period"
  },
  "error": null
}
```

### 10.6 Generate Replacement Suggestions

**Endpoint:** `POST /api/DriverLeave/{leaveId}/suggestions`
**Authorization:** Bearer Token (Admin only)

**Expected Response:**

```json
{
  "success": true,
  "data": {
    "leaveId": "uuid",
    "suggestions": [
      {
        "suggestionId": "uuid",
        "driverId": "550e8400-e29b-41d4-a716-446655440003",
        "driverName": "Jane Driver",
        "vehicleId": "550e8400-e29b-41d4-a716-446655440004",
        "vehiclePlate": "43A-67890",
        "confidenceScore": 0.85,
        "reason": "Available during requested period"
      }
    ],
    "generatedAt": "2024-01-11T10:00:00Z"
  },
  "error": null
}
```

### 10.7 Accept Replacement Suggestion

**Endpoint:** `PUT /api/DriverLeave/{leaveId}/suggestions/{suggestionId}/accept`
**Authorization:** Bearer Token (Admin only)

**Expected Response:**

```json
{
  "success": true,
  "data": {
    "leaveId": "uuid",
    "suggestionId": "uuid",
    "acceptedAt": "2024-01-11T10:30:00Z",
    "replacementDriverId": "550e8400-e29b-41d4-a716-446655440003",
    "replacementVehicleId": "550e8400-e29b-41d4-a716-446655440004"
  },
  "error": null
}
```

### 10.8 Detect Conflicts

**Endpoint:** `GET /api/DriverLeave/{leaveId}/conflicts`
**Authorization:** Bearer Token (Admin only)

**Expected Response:**

```json
{
  "success": true,
  "data": [
    {
      "conflictId": "uuid",
      "conflictType": 1,
      "severity": 2,
      "description": "Overlapping with existing route assignment",
      "affectedEntityId": "550e8400-e29b-41d4-a716-446655440005",
      "affectedEntityType": "Route"
    }
  ],
  "error": null
}
```

---

## 11. Driver Working Hours Management

### 11.1 Create Working Hours

**Endpoint:** `POST /api/DriverWorkingHours`
**Authorization:** Bearer Token (Admin only)

**Request Body:**

```json
{
  "driverId": "550e8400-e29b-41d4-a716-446655440002",
  "dayOfWeek": 1,
  "startTime": "08:00:00",
  "endTime": "17:00:00",
  "isAvailable": true
}
```

**Expected Response:**

```json
{
  "success": true,
  "data": {
    "id": "uuid",
    "driverId": "550e8400-e29b-41d4-a716-446655440002",
    "driverName": "John Driver",
    "dayOfWeek": 1,
    "startTime": "08:00:00",
    "endTime": "17:00:00",
    "isAvailable": true,
    "createdAt": "2024-01-10T10:30:00Z"
  },
  "error": null
}
```

### 11.2 Get Driver Working Hours

**Endpoint:** `GET /api/DriverWorkingHours/driver/{driverId}`
**Authorization:** Bearer Token (Driver or Admin)

**Expected Response:**

```json
{
  "success": true,
  "data": [
    {
      "id": "uuid",
      "driverId": "550e8400-e29b-41d4-a716-446655440002",
      "driverName": "John Driver",
      "dayOfWeek": 1,
      "startTime": "08:00:00",
      "endTime": "17:00:00",
      "isAvailable": true
    }
  ],
  "error": null
}
```

### 11.3 Update Working Hours

**Endpoint:** `PUT /api/DriverWorkingHours/{id}`
**Authorization:** Bearer Token (Driver or Admin)

**Request Body:**

```json
{
  "startTime": "09:00:00",
  "endTime": "18:00:00",
  "isAvailable": true
}
```

### 11.4 Check Driver Availability

**Endpoint:** `GET /api/Driver/available?startTime=2024-01-15T08:00:00Z&endTime=2024-01-15T17:00:00Z`
**Authorization:** Bearer Token (Admin only)

**Expected Response:**

```json
{
  "success": true,
  "data": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440002",
      "firstName": "John",
      "lastName": "Driver",
      "email": "john.driver@edubus.com",
      "phoneNumber": "0123456789"
    }
  ],
  "error": null
}
```

---

## 12. Enhanced Vehicle Assignment Management

### 12.1 Enhanced Driver Assignment

**Endpoint:** `POST /api/Vehicle/{vehicleId}/drivers/enhanced`
**Authorization:** Bearer Token (Admin only)

**Request Body:**

```json
{
  "driverId": "550e8400-e29b-41d4-a716-446655440002",
  "isPrimaryDriver": true,
  "startTimeUtc": "2024-01-15T08:00:00Z",
  "endTimeUtc": "2024-12-31T23:59:59Z",
  "assignmentReason": "Regular route assignment",
  "requireApproval": true,
  "additionalNotes": "Driver has experience with this route",
  "isEmergencyAssignment": false,
  "priorityLevel": 1
}
```

**Expected Response:**

```json
{
  "success": true,
  "data": {
    "assignmentId": "uuid",
    "driverId": "550e8400-e29b-41d4-a716-446655440002",
    "driverName": "John Driver",
    "vehicleId": "550e8400-e29b-41d4-a716-446655440004",
    "vehiclePlate": "43A-67890",
    "isPrimaryDriver": true,
    "startTimeUtc": "2024-01-15T08:00:00Z",
    "endTimeUtc": "2024-12-31T23:59:59Z",
    "assignmentReason": "Regular route assignment",
    "requireApproval": true,
    "additionalNotes": "Driver has experience with this route",
    "isEmergencyAssignment": false,
    "priorityLevel": 1,
    "assignedByAdminId": "550e8400-e29b-41d4-a716-446655440001"
  },
  "error": null
}
```

### 12.2 Detect Assignment Conflicts

**Endpoint:** `GET /api/Vehicle/{vehicleId}/conflicts?startTime=2024-01-15T08:00:00Z&endTime=2024-01-15T17:00:00Z`
**Authorization:** Bearer Token (Admin only)

**Expected Response:**

```json
{
  "success": true,
  "data": [
    {
      "conflictId": "uuid",
      "conflictType": "TimeOverlap",
      "severity": "High",
      "description": "Driver already assigned to another vehicle during this time",
      "conflictingAssignmentId": "uuid",
      "conflictingDriverId": "550e8400-e29b-41d4-a716-446655440002",
      "conflictingVehicleId": "550e8400-e29b-41d4-a716-446655440005"
    }
  ],
  "error": null
}
```

### 12.3 Suggest Replacement for Assignment

**Endpoint:** `POST /api/Vehicle/{vehicleId}/drivers/{assignmentId}/suggest-replacement`
**Authorization:** Bearer Token (Admin only)

**Expected Response:**

```json
{
  "success": true,
  "data": {
    "suggestionId": "uuid",
    "originalDriverId": "550e8400-e29b-41d4-a716-446655440002",
    "suggestedDriverId": "550e8400-e29b-41d4-a716-446655440003",
    "suggestedDriverName": "Jane Driver",
    "confidenceScore": 0.9,
    "reason": "Available during requested period and has similar experience",
    "generatedAt": "2024-01-11T10:00:00Z"
  },
  "error": null
}
```

### 12.4 Approve Assignment

**Endpoint:** `PUT /api/Vehicle/{vehicleId}/drivers/{assignmentId}/approve`
**Authorization:** Bearer Token (Admin only)

**Request Body:**

```json
{
  "note": "Approved after conflict resolution"
}
```

### 12.5 Reject Assignment

**Endpoint:** `PUT /api/Vehicle/{vehicleId}/drivers/{assignmentId}/reject`
**Authorization:** Bearer Token (Admin only)

**Request Body:**

```json
{
  "reason": "Driver not available during requested period"
}
```

---

## 13. Notification System

### 13.1 Get User Notifications

**Endpoint:** `GET /api/notification?page=1&pageSize=20&type=DriverLeaveRequest`
**Authorization:** Bearer Token (All roles)

**Expected Response:**

```json
{
  "success": true,
  "data": [
    {
      "id": "uuid",
      "type": 1,
      "title": "New Leave Request",
      "message": "New leave request from John Driver for Sick Leave from 2024-01-15 to 2024-01-17",
      "priority": 2,
      "status": 0,
      "isRead": false,
      "isAcknowledged": false,
      "requiresAction": true,
      "expiresAt": "2024-01-20T23:59:59Z",
      "createdAt": "2024-01-10T10:30:00Z",
      "metadata": {
        "leaveRequestId": "uuid",
        "driverId": "550e8400-e29b-41d4-a716-446655440002",
        "driverName": "John Driver",
        "leaveType": "SickLeave",
        "startDate": "2024-01-15T00:00:00Z",
        "endDate": "2024-01-17T00:00:00Z"
      }
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 5,
    "totalPages": 1
  },
  "error": null
}
```

### 13.2 Get Unread Count

**Endpoint:** `GET /api/notification/unread-count`
**Authorization:** Bearer Token (All roles)

**Expected Response:**

```json
{
  "success": true,
  "data": {
    "unreadCount": 3
  },
  "error": null
}
```

### 13.3 Mark Notification as Read

**Endpoint:** `PUT /api/notification/{notificationId}/read`
**Authorization:** Bearer Token (All roles)

**Expected Response:**

```json
{
  "success": true,
  "data": {
    "id": "uuid",
    "isRead": true,
    "readAt": "2024-01-11T09:00:00Z"
  },
  "error": null
}
```

### 13.4 Acknowledge Notification

**Endpoint:** `PUT /api/notification/{notificationId}/acknowledge`
**Authorization:** Bearer Token (All roles)

**Expected Response:**

```json
{
  "success": true,
  "data": {
    "id": "uuid",
    "isAcknowledged": true,
    "acknowledgedAt": "2024-01-11T09:00:00Z"
  },
  "error": null
}
```

### 13.5 Mark All as Read

**Endpoint:** `PUT /api/notification/mark-all-read`
**Authorization:** Bearer Token (All roles)

**Expected Response:**

```json
{
  "success": true,
  "data": {
    "updatedCount": 5,
    "updatedAt": "2024-01-11T09:00:00Z"
  },
  "error": null
}
```

### 13.6 Get Admin Notifications

**Endpoint:** `GET /api/notification/admin?page=1&pageSize=20`
**Authorization:** Bearer Token (Admin only)

**Expected Response:**

```json
{
  "success": true,
  "data": [
    {
      "id": "uuid",
      "type": 1,
      "title": "New Leave Request",
      "message": "New leave request from John Driver for Sick Leave from 2024-01-15 to 2024-01-17",
      "priority": 2,
      "status": 0,
      "isRead": false,
      "requiresAction": true,
      "recipientType": 1,
      "createdAt": "2024-01-10T10:30:00Z"
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 10,
    "totalPages": 1
  },
  "error": null
}
```

### 13.7 Get Action Required Notifications

**Endpoint:** `GET /api/notification/action-required`
**Authorization:** Bearer Token (Admin only)

**Expected Response:**

```json
{
  "success": true,
  "data": [
    {
      "id": "uuid",
      "type": 1,
      "title": "New Leave Request",
      "message": "New leave request from John Driver for Sick Leave from 2024-01-15 to 2024-01-17",
      "priority": 2,
      "requiresAction": true,
      "createdAt": "2024-01-10T10:30:00Z"
    }
  ],
  "error": null
}
```

### 13.8 Create Admin Notification

**Endpoint:** `POST /api/notification/admin`
**Authorization:** Bearer Token (Admin only)

**Request Body:**

```json
{
  "type": 4,
  "title": "System Maintenance",
  "message": "Scheduled maintenance will occur on 2024-01-15 from 02:00 to 04:00",
  "priority": 3,
  "requiresAction": false,
  "expiresAt": "2024-01-15T04:00:00Z",
  "metadata": {
    "maintenanceType": "Database",
    "affectedServices": ["API", "Database"],
    "estimatedDuration": "2 hours"
  }
}
```

**Expected Response:**

```json
{
  "success": true,
  "data": {
    "id": "uuid",
    "type": 4,
    "title": "System Maintenance",
    "message": "Scheduled maintenance will occur on 2024-01-15 from 02:00 to 04:00",
    "priority": 3,
    "status": 0,
    "requiresAction": false,
    "expiresAt": "2024-01-15T04:00:00Z",
    "createdAt": "2024-01-10T10:30:00Z"
  },
  "error": null
}
```

### 13.9 Delete Notification

**Endpoint:** `DELETE /api/notification/{notificationId}`
**Authorization:** Bearer Token (All roles)

**Expected Response:**

```json
{
  "success": true,
  "data": {
    "message": "Notification deleted successfully"
  },
  "error": null
}
```

---

## 14. Real-Time Notifications (SignalR)

### 14.1 Connect to Notification Hub

**WebSocket Endpoint:** `/notificationHub`
**Authorization:** Bearer Token required

**Connection URL:**

```
wss://localhost:7061/notificationHub?access_token={jwt_token}
```

**JavaScript Client Example:**

```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("/notificationHub", {
    accessTokenFactory: () => localStorage.getItem("accessToken"),
  })
  .build();

connection.start().then(() => {
  console.log("Connected to Notification Hub");
});
```

### 14.2 Listen for Notifications

**Client-side Event Handlers:**

```javascript
// Receive new notification
connection.on("ReceiveNotification", (notification) => {
  console.log("New notification:", notification);
  // Update UI with new notification
});

// Receive notification count update
connection.on("UpdateNotificationCount", (count) => {
  console.log("Unread count:", count);
  // Update notification badge
});

// Receive admin-specific notifications
connection.on("ReceiveAdminNotification", (notification) => {
  console.log("Admin notification:", notification);
  // Show admin-specific UI
});
```

### 14.3 Test Real-Time Notifications

**Demo Page:** `https://localhost:7061/notification-demo.html`

**Features:**

- Real-time connection status monitoring
- Interactive notification testing
- Connection logs and debugging
- JWT token authentication
- Test notification sending

---

## 15. Background Services

### 15.1 Auto-Replacement Suggestion Service

**Service:** `AutoReplacementSuggestionService`
**Frequency:** Every 15 minutes
**Purpose:** Automatically generates replacement suggestions for pending leave requests

**Process:**

1. Finds pending leave requests with auto-replacement enabled
2. Generates replacement suggestions using existing service
3. Updates leave request with suggestion data
4. Creates admin notifications for review
5. Handles cases with no available replacements

### 15.2 Notification Cleanup Service

**Service:** `NotificationCleanupService`
**Frequency:** Every 6 hours
**Purpose:** Cleans up expired notifications

**Process:**

1. Finds notifications past their expiration date
2. Marks them as expired status
3. Maintains database performance

---

## 16. Enhanced Test Scenarios

### Scenario 1: Complete Driver Leave Workflow

1. **Driver creates leave request**

   - POST /api/DriverLeave
   - Verify notification sent to admin

2. **Admin reviews pending requests**

   - GET /api/DriverLeave/pending
   - GET /api/notification/admin

3. **System generates replacement suggestions**

   - Background service runs automatically
   - POST /api/DriverLeave/{leaveId}/suggestions

4. **Admin approves/rejects with replacement**

   - PUT /api/DriverLeave/{leaveId}/approve
   - PUT /api/DriverLeave/{leaveId}/suggestions/{suggestionId}/accept

5. **Verify notifications sent to driver**
   - GET /api/notification (as driver)

### Scenario 2: Vehicle Assignment with Conflict Detection

1. **Admin assigns driver to vehicle**

   - POST /api/Vehicle/{vehicleId}/drivers/enhanced

2. **System detects conflicts**

   - GET /api/Vehicle/{vehicleId}/conflicts

3. **Admin resolves conflicts**
   - POST /api/Vehicle/{vehicleId}/drivers/{assignmentId}/suggest-replacement
   - PUT /api/Vehicle/{vehicleId}/drivers/{assignmentId}/approve

### Scenario 3: Real-Time Notification Testing

1. **Connect to SignalR hub**

   - Access /notification-demo.html
   - Enter JWT token and connect

2. **Send test notifications**

   - Use demo interface to send notifications
   - Verify real-time delivery

3. **Test notification management**
   - Mark as read, acknowledge, delete
   - Verify count updates in real-time

### Scenario 4: Working Hours Management

1. **Set driver working hours**

   - POST /api/DriverWorkingHours

2. **Check driver availability**

   - GET /api/Driver/available

3. **Verify availability affects assignments**
   - Try to assign unavailable driver
   - Verify conflict detection

---

## 17. Error Scenarios to Test

### 17.1 Driver Leave Errors

- **Invalid date ranges**: End date before start date
- **Overlapping leave requests**: Multiple requests for same period
- **Insufficient notice**: Leave request too close to start date
- **No available replacements**: System cannot find suitable drivers

### 17.2 Vehicle Assignment Errors

- **Driver already assigned**: Assign same driver to multiple vehicles
- **Time conflicts**: Overlapping assignment periods
- **Invalid vehicle status**: Assign to inactive vehicle
- **Driver not available**: Assign during driver's off hours

### 17.3 Notification Errors

- **Expired notifications**: Access notifications past expiration
- **Invalid notification types**: Use non-existent notification types
- **Unauthorized access**: Access other users' notifications
- **Real-time connection failures**: Network issues with SignalR

### 17.4 Background Service Errors

- **Service failures**: Background services stop running
- **Database connection issues**: Services cannot access database
- **Notification delivery failures**: Real-time notifications fail to send
- **Suggestion generation failures**: Auto-replacement service errors
