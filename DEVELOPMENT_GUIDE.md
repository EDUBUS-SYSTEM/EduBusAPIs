# EduBus APIs - Development Guide

## Table of Contents

1. [Getting Started](#getting-started)
2. [Project Structure Overview](#project-structure-overview)
3. [Database Architecture](#database-architecture)
4. [Repository Pattern Usage](#repository-pattern-usage)
5. [Creating Domain Models](#creating-domain-models)
6. [Implementing Services](#implementing-services)
7. [Creating API Controllers](#creating-api-controllers)
8. [Configuration Management](#configuration-management)
9. [Best Practices](#best-practices)
10. [Common Patterns](#common-patterns)
11. [Troubleshooting](#troubleshooting)

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- Visual Studio 2022 or VS Code
- SQL Server (LocalDB or full instance)
- MongoDB (local installation or cloud)

### Setup

1. **Clone the repository**
2. **Configure databases** in `APIs/appsettings.json`
3. **Run the application**: `dotnet run --project APIs`

## Project Structure Overview

```
EduBusAPIs/
├── APIs/                    # Web API Layer
├── Services/                # Business Logic Layer
├── Data/                    # Data Access Layer
├── Utils/                   # Utilities
└── Constants/               # Constants
```

## Database Architecture

### Dual Database Support

The system supports both SQL Server and MongoDB:

- **SQL Server**: For relational data (users, roles, structured data)
- **MongoDB**: For document-based data (logs, analytics, flexible schemas)

### Configuration

```json
{
  "ConnectionStrings": {
    "SqlServer": "Server=localhost;Database=EduBus;Trusted_Connection=true;",
    "MongoDB": "mongodb://localhost:27017"
  },
  "DatabaseSettings": {
    "DefaultDatabase": "SqlServer",
    "UseMultipleDatabases": true
  }
}
```

## Repository Pattern Usage

### Base Models

#### For SQL Server Entities

```csharp
using Data.Models;

public class User : BaseDomain
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}
```

#### For MongoDB Documents

```csharp
using Data.Models;

public class UserLog : BaseMongoDocument
{
    public string UserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
}
```

### Repository Usage Examples

#### SQL Server Repository

```csharp
// In a service or controller
public class UserService
{
    private readonly ISqlRepository<User> _userRepository;

    public UserService(ISqlRepository<User> userRepository)
    {
        _userRepository = userRepository;
    }

    // Get all users
    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await _userRepository.FindAllAsync();
    }

    // Get user by ID
    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await _userRepository.FindAsync(id);
    }

    // Create new user
    public async Task<User> CreateUserAsync(User user)
    {
        return await _userRepository.AddAsync(user);
    }

    // Update user
    public async Task<User?> UpdateUserAsync(User user)
    {
        return await _userRepository.UpdateAsync(user);
    }

    // Soft delete user
    public async Task<User?> DeleteUserAsync(User user)
    {
        return await _userRepository.DeleteAsync(user);
    }

    // Find users by condition
    public async Task<IEnumerable<User>> FindUsersByEmailAsync(string email)
    {
        return await _userRepository.FindByConditionAsync(u => u.Email.Contains(email));
    }

    // Check if user exists
    public async Task<bool> UserExistsAsync(int id)
    {
        return await _userRepository.ExistsAsync(id);
    }
}
```

#### MongoDB Repository

```csharp
// In a service or controller
public class LogService
{
    private readonly IMongoRepository<UserLog> _logRepository;

    public LogService(IMongoRepository<UserLog> logRepository)
    {
        _logRepository = logRepository;
    }

    // Get all logs
    public async Task<IEnumerable<UserLog>> GetAllLogsAsync()
    {
        return await _logRepository.FindAllAsync();
    }

    // Get log by ID
    public async Task<UserLog?> GetLogByIdAsync(string id)
    {
        return await _logRepository.FindAsync(id);
    }

    // Create new log
    public async Task<UserLog> CreateLogAsync(UserLog log)
    {
        return await _logRepository.AddAsync(log);
    }

    // Update log
    public async Task<UserLog?> UpdateLogAsync(UserLog log)
    {
        return await _logRepository.UpdateAsync(log);
    }

    // Soft delete log
    public async Task<UserLog?> DeleteLogAsync(string id)
    {
        return await _logRepository.DeleteAsync(id);
    }

    // Find logs by condition
    public async Task<IEnumerable<UserLog>> FindLogsByUserIdAsync(string userId)
    {
        return await _logRepository.FindByConditionAsync(l => l.UserId == userId);
    }

    // Advanced MongoDB filtering
    public async Task<IEnumerable<UserLog>> FindLogsByActionAsync(string action)
    {
        var filter = Builders<UserLog>.Filter.Eq(l => l.Action, action);
        return await _logRepository.FindByFilterAsync(filter);
    }

    // Paginated results
    public async Task<IEnumerable<UserLog>> GetLogsPaginatedAsync(int page, int pageSize)
    {
        var filter = Builders<UserLog>.Filter.Empty;
        var skip = (page - 1) * pageSize;
        return await _logRepository.FindByFilterAsync(filter, skip, pageSize);
    }

    // Sorted results
    public async Task<IEnumerable<UserLog>> GetLogsSortedByDateAsync()
    {
        var filter = Builders<UserLog>.Filter.Empty;
        var sort = Builders<UserLog>.Sort.Descending(l => l.CreatedAt);
        return await _logRepository.FindByFilterAsync(filter, sort);
    }
}
```

## Creating Domain Models

### SQL Server Entity Example

```csharp
using Data.Models;
using System.ComponentModel.DataAnnotations;

public class Bus : BaseDomain
{
    [Required]
    [StringLength(50)]
    public string BusNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string DriverName { get; set; } = string.Empty;

    [Required]
    public int Capacity { get; set; }

    public string? LicensePlate { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties for relationships
    public virtual ICollection<Route> Routes { get; set; } = new List<Route>();
}

public class Route : BaseDomain
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string StartLocation { get; set; } = string.Empty;

    [Required]
    public string EndLocation { get; set; } = string.Empty;

    public int BusId { get; set; }
    public virtual Bus Bus { get; set; } = null!;
}
```

### MongoDB Document Example

```csharp
using Data.Models;
using MongoDB.Bson.Serialization.Attributes;

public class BusLocation : BaseMongoDocument
{
    [BsonElement("busId")]
    public int BusId { get; set; }

    [BsonElement("latitude")]
    public double Latitude { get; set; }

    [BsonElement("longitude")]
    public double Longitude { get; set; }

    [BsonElement("speed")]
    public double Speed { get; set; }

    [BsonElement("heading")]
    public double Heading { get; set; }

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

## Implementing Services

### Service Interface

```csharp
// Services/Contracts/IBusService.cs
public interface IBusService
{
    Task<IEnumerable<Bus>> GetAllBusesAsync();
    Task<Bus?> GetBusByIdAsync(int id);
    Task<Bus> CreateBusAsync(Bus bus);
    Task<Bus?> UpdateBusAsync(Bus bus);
    Task<Bus?> DeleteBusAsync(int id);
    Task<IEnumerable<Bus>> GetActiveBusesAsync();
    Task<bool> BusExistsAsync(int id);
}
```

### Service Implementation

```csharp
// Services/Implementations/BusService.cs
using Data.Repos.Interfaces;
using Services.Contracts;

public class BusService : IBusService
{
    private readonly ISqlRepository<Bus> _busRepository;

    public BusService(ISqlRepository<Bus> busRepository)
    {
        _busRepository = busRepository;
    }

    public async Task<IEnumerable<Bus>> GetAllBusesAsync()
    {
        return await _busRepository.FindAllAsync();
    }

    public async Task<Bus?> GetBusByIdAsync(int id)
    {
        return await _busRepository.FindAsync(id);
    }

    public async Task<Bus> CreateBusAsync(Bus bus)
    {
        // Add business logic validation
        if (string.IsNullOrWhiteSpace(bus.BusNumber))
        {
            throw new ArgumentException("Bus number is required");
        }

        if (bus.Capacity <= 0)
        {
            throw new ArgumentException("Bus capacity must be greater than 0");
        }

        return await _busRepository.AddAsync(bus);
    }

    public async Task<Bus?> UpdateBusAsync(Bus bus)
    {
        var existingBus = await _busRepository.FindAsync(bus.Id);
        if (existingBus == null)
        {
            return null;
        }

        // Add business logic validation
        if (string.IsNullOrWhiteSpace(bus.BusNumber))
        {
            throw new ArgumentException("Bus number is required");
        }

        return await _busRepository.UpdateAsync(bus);
    }

    public async Task<Bus?> DeleteBusAsync(int id)
    {
        var bus = await _busRepository.FindAsync(id);
        if (bus == null)
        {
            return null;
        }

        return await _busRepository.DeleteAsync(bus);
    }

    public async Task<IEnumerable<Bus>> GetActiveBusesAsync()
    {
        return await _busRepository.FindByConditionAsync(b => b.IsActive);
    }

    public async Task<bool> BusExistsAsync(int id)
    {
        return await _busRepository.ExistsAsync(id);
    }
}
```

## Creating API Controllers

### Controller Example

```csharp
// APIs/Controllers/BusesController.cs
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Data.Models;

[ApiController]
[Route("api/[controller]")]
public class BusesController : ControllerBase
{
    private readonly IBusService _busService;

    public BusesController(IBusService busService)
    {
        _busService = busService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Bus>>> GetBuses()
    {
        try
        {
            var buses = await _busService.GetAllBusesAsync();
            return Ok(buses);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Bus>> GetBus(int id)
    {
        try
        {
            var bus = await _busService.GetBusByIdAsync(id);
            if (bus == null)
            {
                return NotFound(new { message = "Bus not found" });
            }

            return Ok(bus);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<Bus>> CreateBus(Bus bus)
    {
        try
        {
            var createdBus = await _busService.CreateBusAsync(bus);
            return CreatedAtAction(nameof(GetBus), new { id = createdBus.Id }, createdBus);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBus(int id, Bus bus)
    {
        try
        {
            if (id != bus.Id)
            {
                return BadRequest(new { message = "ID mismatch" });
            }

            var updatedBus = await _busService.UpdateBusAsync(bus);
            if (updatedBus == null)
            {
                return NotFound(new { message = "Bus not found" });
            }

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBus(int id)
    {
        try
        {
            var deletedBus = await _busService.DeleteBusAsync(id);
            if (deletedBus == null)
            {
                return NotFound(new { message = "Bus not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<Bus>>> GetActiveBuses()
    {
        try
        {
            var buses = await _busService.GetActiveBusesAsync();
            return Ok(buses);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }
}
```

## Configuration Management

### Service Registration

Add services to `APIs/Program.cs`:

```csharp
// Add service registrations
builder.Services.AddScoped<IBusService, BusService>();
builder.Services.AddScoped<IRouteService, RouteService>();
builder.Services.AddScoped<ILogService, LogService>();
```

### Environment-Specific Configuration

Create `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "SqlServer": "Server=localhost;Database=EduBus_Dev;Trusted_Connection=true;",
    "MongoDB": "mongodb://localhost:27017"
  },
  "MongoDB": {
    "DatabaseName": "EduBusDB_Dev"
  }
}
```

## Best Practices

### 1. Naming Conventions

- **Classes**: PascalCase (e.g., `BusService`, `UserController`)
- **Methods**: PascalCase (e.g., `GetAllBusesAsync`, `CreateUserAsync`)
- **Properties**: PascalCase (e.g., `BusNumber`, `DriverName`)
- **Private fields**: camelCase with underscore (e.g., `_busRepository`)
- **Constants**: UPPER_CASE (e.g., `MAX_CAPACITY`)

### 2. Async/Await Pattern

Always use async/await for I/O operations:

```csharp
// ✅ Good
public async Task<IEnumerable<Bus>> GetAllBusesAsync()
{
    return await _busRepository.FindAllAsync();
}

// ❌ Bad
public IEnumerable<Bus> GetAllBuses()
{
    return _busRepository.FindAllAsync().Result;
}
```

### 3. Error Handling

```csharp
public async Task<Bus?> GetBusByIdAsync(int id)
{
    try
    {
        return await _busRepository.FindAsync(id);
    }
    catch (Exception ex)
    {
        // Log the exception
        _logger.LogError(ex, "Error retrieving bus with ID {BusId}", id);
        throw;
    }
}
```

### 4. Validation

```csharp
public async Task<Bus> CreateBusAsync(Bus bus)
{
    // Input validation
    if (bus == null)
        throw new ArgumentNullException(nameof(bus));

    if (string.IsNullOrWhiteSpace(bus.BusNumber))
        throw new ArgumentException("Bus number is required", nameof(bus));

    if (bus.Capacity <= 0)
        throw new ArgumentException("Capacity must be greater than 0", nameof(bus));

    // Business logic validation
    var existingBus = await _busRepository.FindByConditionAsync(b => b.BusNumber == bus.BusNumber);
    if (existingBus.Any())
        throw new InvalidOperationException("Bus number already exists");

    return await _busRepository.AddAsync(bus);
}
```

### 5. Repository Usage

Choose the appropriate repository based on your entity type:

```csharp
// For SQL Server entities (inherit from BaseDomain)
private readonly ISqlRepository<User> _userRepository;

// For MongoDB documents (inherit from BaseMongoDocument)
private readonly IMongoRepository<UserLog> _logRepository;
```

## Common Patterns

### 1. Service Layer Pattern

```csharp
// Interface
public interface IEntityService<T>
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<T?> GetByIdAsync(int id);
    Task<T> CreateAsync(T entity);
    Task<T?> UpdateAsync(T entity);
    Task<T?> DeleteAsync(int id);
}

// Implementation
public abstract class BaseService<T> : IEntityService<T> where T : BaseDomain
{
    protected readonly ISqlRepository<T> _repository;

    protected BaseService(ISqlRepository<T> repository)
    {
        _repository = repository;
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _repository.FindAllAsync();
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await _repository.FindAsync(id);
    }

    public virtual async Task<T> CreateAsync(T entity)
    {
        return await _repository.AddAsync(entity);
    }

    public virtual async Task<T?> UpdateAsync(T entity)
    {
        return await _repository.UpdateAsync(entity);
    }

    public virtual async Task<T?> DeleteAsync(int id)
    {
        var entity = await _repository.FindAsync(id);
        if (entity == null) return null;
        return await _repository.DeleteAsync(entity);
    }
}
```

### 2. Generic Controller Pattern

```csharp
public abstract class BaseController<T> : ControllerBase where T : BaseDomain
{
    protected readonly IEntityService<T> _service;

    protected BaseController(IEntityService<T> service)
    {
        _service = service;
    }

    [HttpGet]
    public virtual async Task<ActionResult<IEnumerable<T>>> GetAll()
    {
        var entities = await _service.GetAllAsync();
        return Ok(entities);
    }

    [HttpGet("{id}")]
    public virtual async Task<ActionResult<T>> GetById(int id)
    {
        var entity = await _service.GetByIdAsync(id);
        if (entity == null)
            return NotFound();

        return Ok(entity);
    }

    [HttpPost]
    public virtual async Task<ActionResult<T>> Create(T entity)
    {
        var created = await _service.CreateAsync(entity);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public virtual async Task<IActionResult> Update(int id, T entity)
    {
        if (id != entity.Id)
            return BadRequest();

        var updated = await _service.UpdateAsync(entity);
        if (updated == null)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public virtual async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (deleted == null)
            return NotFound();

        return NoContent();
    }
}
```

## Troubleshooting

### Common Issues

1. **Repository not found in DI container**

   - Ensure repository is registered in `Program.cs`
   - Check if using correct repository interface

2. **MongoDB connection issues**

   - Verify MongoDB is running
   - Check connection string in `appsettings.json`
   - Ensure database name is configured

3. **SQL Server connection issues**

   - Verify SQL Server is running
   - Check connection string
   - Ensure database exists

4. **Entity Framework issues**
   - Add migrations: `dotnet ef migrations add InitialCreate`
   - Update database: `dotnet ef database update`

### Debugging Tips

1. **Enable detailed logging**
2. **Use Swagger UI for API testing**
3. **Check database connections**
4. **Verify dependency injection setup**

## Next Steps

1. Implement SQL Server DbContext
2. Create domain models for your business entities
3. Implement business logic services
4. Create API controllers
5. Add authentication and authorization
6. Implement validation and error handling
7. Add unit tests
8. Configure logging and monitoring

This guide provides a foundation for developing with the EduBus APIs architecture. Follow the patterns and best practices outlined to ensure consistent, maintainable code.
