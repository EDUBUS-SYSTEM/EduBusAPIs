# Route API Implementation Summary

## Overview
Đã hoàn thành việc implement các API CRUD cho Route với xóa mềm (soft delete) thông qua việc cập nhật trường `IsActive`.

## Files Created/Modified

### 1. DTO Models
- `Services/Models/Route/RouteDto.cs` - Response DTO cho Route
- `Services/Models/Route/CreateRouteRequest.cs` - Request DTO cho tạo Route
- `Services/Models/Route/UpdateRouteRequest.cs` - Request DTO cho cập nhật Route

### 2. Service Layer
- `Services/Contracts/IRouteService.cs` - Interface cho Route service
- `Services/Implementations/RouteService.cs` - Implementation của Route service

### 3. Controller
- `APIs/Controllers/RouteController.cs` - Đã cập nhật với các endpoint CRUD

### 4. Configuration
- `Services/MapperProfiles/MappingProfile.cs` - Đã thêm AutoMapper mappings
- `APIs/Program.cs` - Đã đăng ký RouteService trong DI container

## API Endpoints

### 1. Create Route
```
POST /api/route
Content-Type: application/json

{
  "routeName": "Route A",
  "vehicleId": "guid",
  "pickupPoints": [
    {
      "pickupPointId": "guid",
      "sequenceOrder": 1
    }
  ]
}
```

### 2. Get Route by ID
```
GET /api/route/{id}
```

### 3. Get All Routes
```
GET /api/route
GET /api/route?activeOnly=true  // Chỉ lấy routes đang active
```

### 4. Update Route
```
PUT /api/route/{id}
Content-Type: application/json

{
  "routeName": "Updated Route Name",
  "vehicleId": "guid",
  "pickupPoints": [
    {
      "pickupPointId": "guid",
      "sequenceOrder": 1
    }
  ]
}
```

### 5. Soft Delete Route
```
DELETE /api/route/{id}
```
**Lưu ý**: Thực hiện xóa mềm bằng cách set `IsDeleted = true`

### 6. Activate Route
```
PATCH /api/route/{id}/activate
```

### 7. Deactivate Route
```
PATCH /api/route/{id}/deactivate
```

## Features Implemented

### 1. Soft Delete
- Xóa mềm thông qua việc set `IsDeleted = true`
- Các API khác sẽ filter ra các route đã bị xóa mềm

### 2. Active/Inactive Management
- Có thể activate/deactivate route thông qua `IsActive` field
- API `GetAllRoutes` có thể filter theo `activeOnly` parameter

### 3. Validation
- Validate route name uniqueness
- Validate required fields
- Validate latitude/longitude ranges
- Validate sequence order uniqueness within route
- **Validate route has pickup points**: Route phải có ít nhất một pickup point
- **Validate vehicle exists**: VehicleId phải tồn tại trong database và chưa bị xóa mềm
- **Validate pickup points exist**: Tất cả PickupPointId trong request phải tồn tại trong database và chưa bị xóa mềm
- **Validate pickup points are assigned to active students**: Chỉ những PickupPoint được gán cho học sinh có status = Active mới có thể sử dụng trong Route
- **Validate pickup points not used in other routes**: PickupPoint không được sử dụng trong các route active khác
- Validate pickup points are not deleted (IsDeleted = false)

### 4. Error Handling
- Proper HTTP status codes
- Detailed error messages
- Exception handling với logging

### 5. Code Quality
- **DRY Principle**: Validation logic được extract thành các method riêng:
  - `ValidatePickupPointsAsync()` - Validate pickup points exist và assigned to active students
  - `ValidatePickupPointsNotInOtherRoutesAsync()` - Validate pickup points not used in other active routes
  - `ValidateSequenceOrderUniqueness()` - Validate sequence order uniqueness
- **Reusable**: Cả Create và Update đều sử dụng cùng validation logic
- **Performance**: Sử dụng Dictionary để lookup pickup points thay vì First()
- **Single Responsibility**: Mỗi validation method có một responsibility riêng biệt

### 6. AutoMapper Integration
- Automatic mapping giữa Entity và DTO
- Support cho nested objects (PickupPointInfo, LocationInfo)

## Database Schema
Route sử dụng MongoDB với BaseMongoDocument:
- `Id`: Guid (Primary Key)
- `RouteName`: string
- `IsActive`: bool (default: true)
- `VehicleId`: Guid
- `PickupPoints`: List<PickupPointInfo>
- `CreatedAt`: DateTime
- `UpdatedAt`: DateTime?
- `IsDeleted`: bool (default: false)

## Notes
- Route sử dụng MongoDB repository pattern
- Tất cả operations đều async/await
- Có validation đầy đủ cho input data
- Error handling và logging được implement đầy đủ
- API responses tuân thủ RESTful conventions
- **Quan trọng**: Route phải có ít nhất một pickup point để có thể hoạt động
- **Quan trọng**: VehicleId phải tồn tại trong database trước khi tạo Route
- **Quan trọng**: PickupPoint phải tồn tại trước khi tạo Route - hệ thống sẽ validate tất cả PickupPointId trong request
- **Quan trọng**: Chỉ những PickupPoint được gán cho học sinh có status = Active mới có thể sử dụng trong Route (học sinh đã đăng ký dịch vụ)
- **Quan trọng**: PickupPoint không được sử dụng trong các route active khác (tránh conflict)
- Sequence order phải unique trong mỗi route
- **Location info được tự động lấy từ PickupPoint**: Không cần truyền latitude, longitude, address trong request vì sẽ được lấy từ PickupPoint đã tồn tại
