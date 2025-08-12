# EduBus APIs - Project Architecture Documentation

## Overview

EduBus APIs is a .NET 8.0 backend solution designed for an educational bus management system. The project follows a clean architecture pattern with clear separation of concerns across multiple projects within a solution structure.

## Solution Structure

The solution is organized into multiple projects, each with a specific responsibility:

```
EduBusAPIs/
├── EduBusAPIs.sln                 # Main solution file
├── APIs/                          # Web API Layer (Presentation)
├── Services/                      # Business Logic Layer
├── Data/                          # Data Access Layer
├── Utils/                         # Utility/Helper Layer
└── Constants/                     # Constants and Configuration
```

## Project Details

### 1. APIs Project (Presentation Layer)

**Location**: `APIs/`
**Type**: ASP.NET Core Web API
**Target Framework**: .NET 8.0

**Purpose**:

- Entry point for HTTP requests
- API controllers and endpoints
- Request/response handling
- Swagger documentation

**Key Files**:

- `Program.cs` - Application startup and configuration
- `appsettings.json` - Application configuration
- `APIs.csproj` - Project dependencies (Swashbuckle.AspNetCore)
- `Controllers/` - API controllers (currently empty)

**Dependencies**:

- Swashbuckle.AspNetCore (v6.4.0) - For API documentation

**Configuration**:

- Swagger/OpenAPI enabled for development
- HTTPS redirection
- Authorization middleware configured

### 2. Services Project (Business Logic Layer)

**Location**: `Services/`
**Type**: Class Library
**Target Framework**: .NET 8.0

**Purpose**:

- Business logic implementation
- Service contracts and interfaces
- Data mapping and transformation
- Business rules and validation

**Structure**:

```
Services/
├── Contracts/         # Service interfaces
├── Implementations/   # Service implementations
├── Models/           # Business models/DTOs
└── MapperProfiles/   # AutoMapper profiles
```

**Current State**: All directories are empty, indicating the business logic layer is not yet implemented.

### 3. Data Project (Data Access Layer)

**Location**: `Data/`
**Type**: Class Library
**Target Framework**: .NET 8.0

**Purpose**:

- Database models and entities
- Repository pattern implementation
- Data access operations
- Entity Framework Core integration

**Structure**:

```
Data/
├── Models/
│   └── BaseDomain.cs    # Base entity class
└── Repos/
    ├── IRepository.cs   # Generic repository interface
    └── Repository.cs    # Generic repository implementation
```

**Key Components**:

#### BaseDomain Class

```csharp
public class BaseDomain
{
    public int Id { get; set; }
}
```

- Base class for all domain entities
- Provides common Id property for all entities

#### Repository Pattern

**Interface**: `IRepository<T>`

- Generic repository interface with CRUD operations
- Supports async operations
- Includes conditional querying capabilities

**Implementation**: `Repository<T>`

- Generic repository implementation using Entity Framework Core
- Supports:
  - CRUD operations (Create, Read, Update, Delete)
  - Conditional queries with expressions
  - Include operations for related entities
  - Concurrency handling
  - Entity tracking and change detection

**Key Features**:

- Async/await pattern throughout
- Entity Framework Core integration
- Generic implementation for type safety
- Concurrency exception handling
- Include support for eager loading

### 4. Utils Project (Utility Layer)

**Location**: `Utils/`
**Type**: Class Library
**Target Framework**: .NET 8.0

**Purpose**:

- Helper classes and utilities
- Common functionality
- Extension methods
- Shared utilities across projects

**Current State**: Empty project structure, ready for utility implementations.

### 5. Constants Project (Configuration Layer)

**Location**: `Constants/`
**Type**: Class Library
**Target Framework**: .NET 8.0

**Purpose**:

- Application constants
- Configuration values
- Enums and static data
- Shared constants across projects

**Current State**: Empty project structure, ready for constant definitions.

## Architecture Patterns

### 1. Clean Architecture

The project follows clean architecture principles with clear separation of concerns:

- **Presentation Layer** (APIs): Handles HTTP requests and responses
- **Business Layer** (Services): Contains business logic and rules
- **Data Layer** (Data): Manages data access and persistence
- **Infrastructure Layer** (Utils, Constants): Provides utilities and configuration

### 2. Repository Pattern

- Generic repository interface and implementation
- Abstraction over data access
- Supports multiple entity types through generics
- Async operations for better performance

### 3. Dependency Injection

- Built-in .NET dependency injection container
- Service registration in Program.cs
- Loose coupling between layers

### 4. Generic Repository Pattern

- Type-safe repository operations
- Reusable across different entity types
- Consistent CRUD operations

## Technology Stack

### Core Technologies

- **.NET 8.0**: Latest LTS version of .NET
- **ASP.NET Core**: Web framework for building APIs
- **Entity Framework Core**: ORM for data access
- **Swagger/OpenAPI**: API documentation

### Project Dependencies

- **APIs Project**: Swashbuckle.AspNetCore for API documentation
- **Data Project**: Entity Framework Core (implicitly used)
- **Services, Utils, Constants**: No external dependencies currently

## Development Status

### Completed Components

- ✅ Solution structure and project setup
- ✅ Basic API configuration with Swagger
- ✅ Repository pattern implementation
- ✅ Base domain model
- ✅ Generic repository with full CRUD operations

### Pending Implementation

- ❌ API Controllers
- ❌ Business logic services
- ❌ Domain models and entities
- ❌ Data transfer objects (DTOs)
- ❌ AutoMapper profiles
- ❌ Database context configuration
- ❌ Authentication and authorization
- ❌ Validation and error handling
- ❌ Logging configuration
- ❌ Utility classes
- ❌ Application constants

## Configuration

### Application Settings

- **Development Environment**: Swagger enabled
- **Logging**: Information level for default, Warning for ASP.NET Core
- **HTTPS**: Redirection enabled
- **Authorization**: Middleware configured but not implemented

### Database Configuration

- Entity Framework Core is referenced but not configured
- No connection string defined
- No DbContext implementation

## API Documentation

The project includes Swagger/OpenAPI documentation:

- **Development URL**: Available at `/swagger` when running in development
- **HTTP Test File**: `APIs.http` contains sample API calls for testing

## Next Steps for Development

1. **Database Setup**

   - Create DbContext class
   - Configure connection strings
   - Add Entity Framework migrations

2. **Domain Models**

   - Create specific domain entities
   - Define relationships between entities
   - Add validation attributes

3. **Business Logic**

   - Implement service interfaces
   - Create business logic implementations
   - Add validation and business rules

4. **API Controllers**

   - Create RESTful API controllers
   - Implement CRUD endpoints
   - Add proper HTTP status codes

5. **Data Transfer Objects**

   - Create DTOs for API requests/responses
   - Implement AutoMapper profiles
   - Separate internal models from external contracts

6. **Authentication & Authorization**

   - Implement JWT authentication
   - Add role-based authorization
   - Secure API endpoints

7. **Error Handling**

   - Implement global exception handling
   - Create custom error responses
   - Add logging and monitoring

8. **Testing**
   - Unit tests for business logic
   - Integration tests for APIs
   - Repository tests

## Best Practices Implemented

1. **Separation of Concerns**: Clear project boundaries
2. **Generic Repository**: Reusable data access pattern
3. **Async/Await**: Non-blocking operations
4. **Dependency Injection**: Loose coupling
5. **Clean Architecture**: Layered design
6. **Type Safety**: Generic implementations

## Development Guidelines

1. **Naming Conventions**: Follow C# naming conventions
2. **Async Operations**: Use async/await for I/O operations
3. **Error Handling**: Implement proper exception handling
4. **Documentation**: Use XML comments for public APIs
5. **Testing**: Write unit tests for business logic
6. **Security**: Implement proper authentication and authorization

This architecture provides a solid foundation for building a scalable and maintainable educational bus management system.
