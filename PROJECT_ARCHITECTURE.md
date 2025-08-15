# EduBus APIs - Project Architecture Documentation

## Overview

EduBus APIs is a .NET 8.0 backend solution designed for an educational bus management system. The project follows a clean architecture pattern with clear separation of concerns across multiple projects within a solution structure. The system supports dual database architecture with both SQL Server and MongoDB for flexible data storage.

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
- Dependency injection configuration

**Key Files**:

- `Program.cs` - Application startup and configuration
- `appsettings.json` - Application configuration with database settings
- `APIs.csproj` - Project dependencies
- `Controllers/` - API controllers (currently empty)

**Dependencies**:

- Swashbuckle.AspNetCore (v6.4.0) - For API documentation
- Data project - For data access
- Services project - For business logic
- Utils project - For utilities

**Configuration**:

- Swagger/OpenAPI enabled for development
- HTTPS redirection
- Authorization middleware configured
- Dual database support (SQL Server + MongoDB)
- Repository pattern registration

**Database Configuration**:

```json
{
  "ConnectionStrings": {
    "SqlServer": "Server=localhost;Database=EduBus;Trusted_Connection=true;TrustServerCertificate=true;",
    "MongoDB": "mongodb://localhost:27017"
  },
  "MongoDB": {
    "DatabaseName": "EduBusDB"
  },
  "DatabaseSettings": {
    "DefaultDatabase": "SqlServer",
    "UseMultipleDatabases": true
  }
}
```

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

**Dependencies**:

- Data project - For data access

**Current State**: All directories are empty, indicating the business logic layer is not yet implemented.

### 3. Data Project (Data Access Layer)

**Location**: `Data/`
**Type**: Class Library
**Target Framework**: .NET 8.0

**Purpose**:

- Database models and entities
- Repository pattern implementation
- Data access operations
- Dual database support (SQL Server + MongoDB)

**Dependencies**:

- Microsoft.EntityFrameworkCore (v9.0.8)
- Microsoft.EntityFrameworkCore.SqlServer (v9.0.8)
- MongoDB.Bson (v3.4.2)
- MongoDB.Driver (v3.4.2)

**Structure**:

```
Data/
├── Models/
│   ├── BaseDomain.cs           # Base entity class for SQL Server
│   └── BaseMongoDocument.cs    # Base document class for MongoDB
├── Contexts/
│   ├── MongoDB/
│   │   └── EduBusMongoContext.cs
│   └── SqlServer/              # Empty (DbContext to be implemented)
└── Repos/
    ├── Interfaces/
    │   ├── IMongoRepository.cs
    │   └── ISqlRepository.cs
    ├── MongoDB/
    │   └── MongoRepository.cs
    └── SqlServer/
        └── SqlRepository.cs
```

#### Base Models

**BaseDomain Class** (SQL Server entities):

```csharp
public class BaseDomain
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
}
```

**BaseMongoDocument Class** (MongoDB documents):

```csharp
public class BaseMongoDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime? UpdatedAt { get; set; }

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; } = false;
}
```

#### Database Contexts

**EduBusMongoContext**:

- MongoDB connection management
- Database and collection access
- Connection health monitoring
- Index management capabilities
- Configuration-based connection string and database name

**Features**:

- Connection string validation
- Database health checks
- Collection management
- Index creation support
- Error handling

#### Repository Pattern

**IMongoRepository Interface**:

- Generic repository interface for MongoDB documents
- Full CRUD operations
- Advanced filtering and sorting
- Pagination support
- Atomic operations

**Methods**:

- `FindAllAsync()` - Get all non-deleted documents
- `FindAsync(string id)` - Find by ID
- `AddAsync(T document)` - Create new document
- `UpdateAsync(T document)` - Update existing document
- `DeleteAsync(string id)` - Soft delete by ID
- `FindByConditionAsync(Expression<Func<T, bool>>)` - Conditional queries
- `FindByFilterAsync(FilterDefinition<T>)` - MongoDB filter queries
- `FindByFilterAsync(FilterDefinition<T>, SortDefinition<T>)` - Filtered and sorted queries
- `FindByFilterAsync(FilterDefinition<T>, int skip, int limit)` - Paginated queries
- `GetCountAsync()` - Document count
- `ExistsAsync(string id)` - Check existence
- `FindOneAndUpdateAsync()` - Atomic update operations
- `FindOneAndDeleteAsync()` - Atomic delete operations

**ISqlRepository Interface**:

- Generic repository interface for SQL Server entities
- Full CRUD operations
- Include support for related entities
- Conditional queries

**Methods**:

- `FindAllAsync()` - Get all non-deleted entities
- `FindAllAsync(params Expression<Func<T, object>>[] includes)` - With includes
- `FindAsync(int id)` - Find by ID
- `AddAsync(T entity)` - Create new entity
- `UpdateAsync(T entity)` - Update existing entity
- `DeleteAsync(T entity)` - Soft delete entity
- `FindByConditionAsync(Expression<Func<T, bool>>)` - Conditional queries
- `FindByConditionAsync(Expression<Func<T, bool>>, params Expression<Func<T, object>>[] includes)` - With includes
- `GetCountAsync()` - Entity count
- `ExistsAsync(int id)` - Check existence

**MongoRepository Implementation**:

- Full implementation of IMongoRepository
- Soft delete support
- Automatic timestamp management
- Reflection-based property updates
- Comprehensive error handling

**SqlRepository Implementation**:

- Full implementation of ISqlRepository
- Entity Framework Core integration
- Soft delete support
- Concurrency handling
- Include support for related entities

### 4. Utils Project (Utility Layer)

**Location**: `Utils/`
**Type**: Class Library
**Target Framework**: .NET 8.0

**Purpose**:

- Helper classes and utilities
- Database factory pattern
- Common functionality
- Shared utilities across projects

**Dependencies**:

- Microsoft.Extensions.Configuration.Abstractions (v9.0.0)
- Microsoft.Extensions.DependencyInjection.Abstractions (v9.0.0)

**Key Components**:

**DatabaseFactory**:

- Factory pattern for database selection
- Configuration-based database switching
- Support for multiple database types
- Service provider integration

**Features**:

- `DatabaseType` enum (SqlServer, MongoDB)
- `IDatabaseFactory` interface
- Configuration-based default database selection
- Multiple database support toggle
- Repository resolution by database type

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

- Generic repository interfaces and implementations
- Abstraction over data access
- Support for multiple database types
- Async operations for better performance
- Soft delete support

### 3. Factory Pattern

- DatabaseFactory for repository resolution
- Configuration-based database selection
- Support for multiple database types
- Service provider integration

### 4. Dual Database Architecture

- SQL Server support with Entity Framework Core
- MongoDB support with MongoDB.Driver
- Configuration-based database selection
- Repository pattern abstraction

### 5. Dependency Injection

- Built-in .NET dependency injection container
- Service registration in Program.cs
- Repository pattern registration
- Loose coupling between layers

## Technology Stack

### Core Technologies

- **.NET 8.0**: Latest LTS version of .NET
- **ASP.NET Core**: Web framework for building APIs
- **Entity Framework Core**: ORM for SQL Server
- **MongoDB.Driver**: Official MongoDB driver for .NET
- **Swagger/OpenAPI**: API documentation

### Database Technologies

- **SQL Server**: Relational database with Entity Framework Core
- **MongoDB**: NoSQL database with MongoDB.Driver
- **Dual Database Support**: Configuration-based database selection

### Project Dependencies

- **APIs Project**:
  - Swashbuckle.AspNetCore for API documentation
  - Data, Services, Utils project references
- **Data Project**:
  - Entity Framework Core for SQL Server
  - MongoDB.Driver for MongoDB
- **Services Project**:
  - Data project reference
- **Utils Project**:
  - Microsoft.Extensions.Configuration.Abstractions
  - Microsoft.Extensions.DependencyInjection.Abstractions
- **Constants Project**:
  - No external dependencies

## Development Status

### Completed Components

- ✅ Solution structure and project setup
- ✅ Basic API configuration with Swagger
- ✅ Dual database support (SQL Server + MongoDB)
- ✅ Repository pattern implementation for both databases
- ✅ Base domain models for both database types
- ✅ MongoDB context and connection management
- ✅ Generic repository with full CRUD operations
- ✅ Soft delete support
- ✅ Database factory pattern
- ✅ Configuration-based database selection
- ✅ Dependency injection setup

### Pending Implementation

- ❌ SQL Server DbContext implementation
- ❌ API Controllers
- ❌ Business logic services
- ❌ Domain models and entities
- ❌ Data transfer objects (DTOs)
- ❌ AutoMapper profiles
- ❌ Authentication and authorization
- ❌ Validation and error handling
- ❌ Logging configuration
- ❌ Application constants
- ❌ Unit tests

## Configuration

### Application Settings

- **Development Environment**: Swagger enabled
- **Logging**: Information level for default, Warning for ASP.NET Core
- **HTTPS**: Redirection enabled
- **Authorization**: Middleware configured but not implemented

### Database Configuration

- **SQL Server**: Entity Framework Core configured
- **MongoDB**: MongoDB.Driver configured
- **Connection Strings**: Configured in appsettings.json
- **Database Selection**: Configuration-based with fallback to SQL Server

## API Documentation

The project includes Swagger/OpenAPI documentation:

- **Development URL**: Available at `/swagger` when running in development
- **HTTP Test File**: `APIs.http` contains sample API calls for testing

## Next Steps for Development

1. **SQL Server Implementation**

   - Create DbContext class
   - Add Entity Framework migrations
   - Configure SQL Server repository registration

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
2. **Repository Pattern**: Reusable data access pattern
3. **Factory Pattern**: Database selection abstraction
4. **Async/Await**: Non-blocking operations
5. **Dependency Injection**: Loose coupling
6. **Clean Architecture**: Layered design
7. **Type Safety**: Generic implementations
8. **Soft Delete**: Data integrity preservation
9. **Dual Database Support**: Flexible data storage
10. **Configuration Management**: Environment-based settings

## Development Guidelines

1. **Naming Conventions**: Follow C# naming conventions
2. **Async Operations**: Use async/await for I/O operations
3. **Error Handling**: Implement proper exception handling
4. **Documentation**: Use XML comments for public APIs
5. **Testing**: Write unit tests for business logic
6. **Security**: Implement proper authentication and authorization
7. **Database Selection**: Use configuration-based database selection
8. **Repository Usage**: Use appropriate repository based on entity type

This architecture provides a solid foundation for building a scalable and maintainable educational bus management system with flexible database support.
