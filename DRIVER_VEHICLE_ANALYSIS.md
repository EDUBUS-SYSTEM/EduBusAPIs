# Driver and Vehicle Changes – Detailed Technical Analysis

## Overview

This document analyzes all recent changes related to driver and vehicle domains across the EduBus backend (.NET 8, ASP.NET Core Web API). It covers models, repositories, services, controllers, background interactions, validations, security, and integration points relevant to scheduling, availability, assignments, and driver leave workflows.

## Domain Models

- DriverWorkingHours

  - Fields: `DriverId`, `DayOfWeek`, `StartTime`, `EndTime`, `IsAvailable`, navigation `Driver`.
  - Purpose: Define per-day availability windows to support availability queries and assignment validation.

- DriverVehicle (assignment)

  - Fields (key): `DriverId`, `VehicleId`, `IsPrimaryDriver`, `StartTimeUtc`, `EndTimeUtc?`, `AssignedByAdminId`, audit fields.
  - Purpose: Persist driver-to-vehicle assignment history with active-window semantics and primary driver flag.

- DriverLeaveRequest

  - Additions: `AutoReplacementEnabled`, optional `SuggestedReplacementDriverId`, `SuggestedReplacementVehicleId`, `SuggestionGeneratedAt`.
  - Purpose: Enable admin-reviewed auto-replacement suggestions during leave workflows.

- DriverLeaveConflict

  - Purpose: Capture conflicts between leave and trips with replacement suggestion placeholders (driver/vehicle, score, reason) to aid resolutions.

- Vehicle
  - License plate is stored hashed; decrypted for responses via `SecurityHelper` at service layer.

## Repositories (SQL, EF Core)

- IDriverVehicleRepository / DriverVehicleRepository

  - Queries:
    - `GetByVehicleIdAsync(vehicleId, isActive?)` with active-window filter (now within [start, end)).
    - `IsDriverAlreadyAssignedAsync(vehicleId, driverId, onlyActive)` to prevent duplicate active assignments.
    - Conflict checks: `HasTimeConflictAsync(driverId, start, end?)`, `HasVehicleTimeConflictAsync(vehicleId, start, end?)`.
  - Write:
    - `AssignDriverAsync(DriverVehicle)` sets `CreatedAt` and persists assignment.

- IDriverWorkingHoursRepository / DriverWorkingHoursRepository

  - Queries:
    - `GetByDriverIdAsync(driverId)` and `GetByDriverAndDayAsync(driverId, day)`.
    - Availability helpers: `IsDriverAvailableAtTimeAsync`, `GetAvailableDriversAtTimeAsync`, `GetDriversAvailableInTimeRangeAsync`.

- IVehicleRepository / VehicleRepository
  - Filtering, pagination, and sorting for admin views; used by `VehicleService`.

## Services

- DriverService

  - Availability:
    - `IsDriverAvailableAsync(driverId, start, end)` combines working-hours window check and `DriverVehicleRepository.HasTimeConflictAsync`.
    - `GetAvailableDriversAsync(start, end)` filters all drivers via the above.

- DriverWorkingHoursService

  - CRUD-like operations for working hours:
    - `CreateWorkingHoursAsync`, `UpdateWorkingHoursAsync`, `DeleteWorkingHoursAsync`.
  - Queries and utilities (per interface): `GetWorkingHoursByDriverAsync`, `GetWorkingHoursByDriverAndDayAsync`, bulk helpers (`SetDefaultWorkingHoursAsync`, `CopyWorkingHoursFromDriverAsync`).

- DriverVehicleService

  - `GetDriversByVehicleAsync(vehicleId, isActive?)`: returns current/expired assignments optionally filtered by activity.
  - `AssignDriverAsync(vehicleId, dto, adminId)`:
    - Validates time window (`EndTimeUtc > StartTimeUtc` when present).
    - Prevents duplicate active assignments for the same driver/vehicle.
    - Persists assignment and returns DTO (includes `AssignedByAdminId`).
  - Conflict detection: `DetectAssignmentConflictsAsync(vehicleId, start, end)` leverages repo vehicle time conflict, returns structured conflict DTOs.
  - Stubs/contract for extended flows: `AssignDriverWithValidationAsync`, `UpdateAssignmentAsync`, `CancelAssignmentAsync`, `SuggestReplacementAsync`, `AcceptReplacementSuggestionAsync`, `ApproveAssignmentAsync`, `RejectAssignmentAsync` (some implemented, some for future extensions).

- VehicleService
  - On read, decrypts hashed license plate via `SecurityHelper.DecryptFromBytes` for client-safe DTO presentation.
  - Provides filtering/pagination for admin listings.

## Controllers (API Layer)

- DriverController

  - Admin driver listing and retrieval.
  - Status management endpoints (suspend/reactivate, get by status).
  - Availability endpoint: `GET /api/Driver/available?startTime&endTime` consuming `DriverService.GetAvailableDriversAsync`.
  - Driver working hours:
    - `GET /api/Driver/{id}/working-hours` with access checks.
    - `POST /api/Driver/{id}/working-hours` to set working hours for self (driver) or any (admin), mapping to `CreateWorkingHoursAsync`.
  - Driver leave (integration touchpoint): `GET /api/Driver/{id}/leaves` (authorization helper enforces self-access unless admin).

- VehicleController
  - CRUD: list (admin), get by id, create (admin), full update (admin), partial update (admin), soft delete (admin).
  - Assignments:
    - `GET /api/Vehicle/{vehicleId}/drivers?isActive=true|false` to list assignments.
    - `POST /api/Vehicle/{vehicleId}/drivers` to assign driver with time window and primary flag (admin only); returns 201 with assignment summary.

## Validation and Business Rules

- Time window correctness: assignment `EndTimeUtc` must be null or greater than `StartTimeUtc`.
- Duplicate active assignment prevention per vehicle/driver.
- Availability computation uses both working hours and assignment conflicts.
- Soft-delete semantics preserved for vehicle and assignment entities.
- Decryption of license plate only at service boundary; storage remains hashed for security.

## Security and Authorization

- All endpoints require authentication; admin-only scopes enforced via `[Authorize(Roles = Roles.Admin)]` where applicable.
- Driver self-service operations (e.g., working hours, viewing own leaves) guarded by `AuthorizationHelper.CanAccessUserData`.

## Integration Points

- Leave workflow

  - When creating or managing leaves, notifications are sent (admin review), and background jobs generate replacement suggestions leveraging driver availability and assignment data.
  - Conflicts (leave vs. trips) recorded in `DriverLeaveConflict`, enabling admin decisions and future automated resolution.

- Background services
  - AutoReplacementSuggestionService periodically evaluates pending leaves, generates suggestions, and populates suggestion fields or notifications for admins.

## Endpoints Summary (Driver/Vehicle Focus)

- Driver

  - `GET /api/Driver/available?startTime&endTime` – Query available drivers in a time range.
  - `GET /api/Driver/{id}/working-hours` – View working hours (self or admin).
  - `POST /api/Driver/{id}/working-hours` – Set working hours (self or admin).
  - Status management: suspend/reactivate, list by status (admin).

- Vehicle
  - `GET /api/Vehicle` – Admin list with filters and pagination.
  - `GET /api/Vehicle/{vehicleId}` – Vehicle details (license plate decrypted in response).
  - `POST /api/Vehicle` – Create (admin).
  - `PUT /api/Vehicle/{vehicleId}` – Full update (admin).
  - `PATCH /api/Vehicle/{vehicleId}` – Partial update (admin).
  - `DELETE /api/Vehicle/{vehicleId}` – Soft delete (admin).
  - Assignments:
    - `GET /api/Vehicle/{vehicleId}/drivers?isActive=` – List assignments.
    - `POST /api/Vehicle/{vehicleId}/drivers` – Assign a driver with window and primary flag (admin).

## Data Flow Highlights

1. Availability query

   - Controller → DriverService → DriverWorkingHoursRepository + DriverVehicleRepository → aggregate checks → DTO list.

2. Assign driver to vehicle

   - Controller (admin) → DriverVehicleService → validation + duplicate/overlap checks → persist assignment → return assignment DTO.

3. Leave replacement support
   - Leave creation/approval triggers notification and/or background job; suggestions are computed using availability and assignment repositories.

## Risks, Gaps, and Recommendations

- Overlap detection breadth

  - Current `DetectAssignmentConflictsAsync` handles vehicle time conflicts; extend to driver-level overlapping across multiple vehicles and routes.

- Assignment lifecycle

  - Implement `UpdateAssignmentAsync`, `ApproveAssignmentAsync`, `RejectAssignmentAsync` consistently (status, audit, notifications).

- Replacement suggestions for assignments

  - Implement `SuggestReplacementAsync` to mirror leave suggestions using availability, performance, and familiarity scorers already outlined in repositories.

- Working hours UX and defaults

  - Implement `SetDefaultWorkingHoursAsync` and `CopyWorkingHoursFromDriverAsync` at service/controller to reduce admin effort.

- License plate handling

  - Ensure encryption/decryption keys rotation strategy and auditing for access to decrypted data.

- Pagination and filtering
  - Ensure uniform pagination and sorting across vehicle and assignment listings for scalability.

## Conclusion

The driver and vehicle subsystems now provide a robust base for scheduling: availability derives from working hours and assignments; assignment operations enforce key constraints; vehicle data is secured at rest; and leave workflows are integrated with suggestions and conflicts. Completing the noted extensions will further strengthen operational readiness and admin workflows.
