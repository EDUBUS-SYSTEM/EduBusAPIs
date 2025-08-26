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
2. **Configure databases** using User Secrets (for local development)
3. **Run the application**: `dotnet run --project APIs`

#### Setting up User Secrets for Local Development

**Step 1: Initialize User Secrets**

```bash
cd APIs
dotnet user-secrets init
```

**Step 2: Add Database Connection Strings**

```bash
# SQL Server Connection String (Windows Authentication)
dotnet user-secrets set "ConnectionStrings:SqlServer" "Server=;Database=edubus_dev;Trusted_Connection=True;Encrypt=false"

# SQL Server Connection String (SQL Authentication - Alternative)
# dotnet user-secrets set "ConnectionStrings:SqlServer" "Server=localhost;Database=edubus_dev;User Id=sa;Password=your_password;TrustServerCertificate=True"

# MongoDB Connection String
dotnet user-secrets set "ConnectionStrings:MongoDb" "mongodb://localhost:27017/edubus"
```

dotnet user-secrets set "Jwt:Key" "super-secret-key-for-EduBus-123789@@"

```bash
dotnet user-secrets set "EmailSettings:SmtpServer" "smtp.gmail.com"
dotnet user-secrets set "EmailSettings:SmtpPort" "587"
dotnet user-secrets set "EmailSettings:SenderEmail" "edubusfuda@gmail.com"
dotnet user-secrets set "EmailSettings:SenderName" "EduBus"
dotnet user-secrets set "EmailSettings:Username" "edubusfuda@gmail.com"
dotnet user-secrets set "EmailSettings:Password" "pzdn bovm zlvl zadj"
dotnet user-secrets set "EmailSettings:EnableSsl" "true"
```

**Step 3: Verify User Secrets**

```bash
dotnet user-secrets list
```

**Alternative: Manual Configuration**
If you prefer to use `appsettings.json` directly (not recommended for production):

```json
{
  "ConnectionStrings": {
    "SqlServer": "Server=LAPTOP-DVKPB8S9;Database=edubus_dev;Trusted_Connection=True;Encrypt=false",
    "MongoDb": "mongodb://localhost:27017/edubus"
  },
  "DatabaseSettings": {
    "DefaultDatabase": "SqlServer",
    "UseMultipleDatabases": true
  }
}
```

**Note**: User Secrets are automatically loaded in Development environment and override `appsettings.json` values.

#### Current User Secrets Configuration

Based on the current project setup, the following User Secrets are configured:

```bash
# Current User Secrets (as of project setup)
ConnectionStrings:SqlServer = Server=LAPTOP-DVKPB8S9;Database=edubus_dev;Trusted_Connection=True;Encrypt=
ConnectionStrings:MongoDb = mongodb://localhost:27017/edubus
```

**Configuration Details:**

1. **SQL Server Connection**:

   - **Server**: `LAPTOP-DVKPB8S9` (local SQL Server instance)
   - **Database**: `edubus_dev` (development database)
   - **Authentication**: Windows Authentication (`Trusted_Connection=True`)
   - **Encryption**: Disabled (`Encrypt=false`)

2. **MongoDB Connection**:
   - **Server**: `localhost:27017` (local MongoDB instance)
   - **Database**: `edubus` (MongoDB database name)

**To customize for your environment:**

```bash
# For different SQL Server instance
dotnet user-secrets set "ConnectionStrings:SqlServer" "Server=YOUR_SERVER_NAME;Database=edubus_dev;Trusted_Connection=True;Encrypt=false"

# For SQL Authentication instead of Windows Authentication
dotnet user-secrets set "ConnectionStrings:SqlServer" "Server=YOUR_SERVER_NAME;Database=edubus_dev;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True"

# For different MongoDB instance
dotnet user-secrets set "ConnectionStrings:MongoDb" "mongodb://YOUR_MONGO_HOST:27017/edubus"
```

## Project Structure Overview

```
EduBusAPIs/
├── APIs/                    # Web API Layer
│   ├── Controllers/         # API Controllers
│   ├── Program.cs          # Application startup
│   └── appsettings.json    # Configuration
├── Services/                # Business Logic Layer
│   ├── Contracts/          # Service interfaces
│   ├── Implementations/    # Service implementations
│   ├── Models/             # Business models/DTOs
│   └── MapperProfiles/     # AutoMapper profiles
├── Data/                    # Data Access Layer
│   ├── Models/             # Domain models
│   ├── Contexts/           # Database contexts
│   │   ├── SqlServer/      # Entity Framework context
│   │   └── MongoDB/        # MongoDB context
│   ├── Repos/              # Repository implementations
│   │   ├── Interfaces/     # Repository interfaces
│   │   ├── SqlServer/      # SQL Server repositories
│   │   └── MongoDB/        # MongoDB repositories
│   └── Migrations/         # Entity Framework migrations
├── Utils/                   # Utilities
│   └── DatabaseFactory.cs  # Database factory pattern
└── Constants/               # Constants
```

## Database Architecture

### Dual Database Support

The system supports both SQL Server and MongoDB:

- **SQL Server**: For relational data (users, students, routes, structured data)
- **MongoDB**: For document-based data (notifications, logs, flexible schemas)

### Configuration

```json
{
  "ConnectionStrings": {
    "SqlServer": "Server=LAPTOP-DVKPB8S9;Database=edubus_dev;Trusted_Connection=True;Encrypt=false",
    "MongoDb": "mongodb://localhost:27017/edubus"
  },
  "DatabaseSettings": {
    "DefaultDatabase": "SqlServer",
    "UseMultipleDatabases": true
  }
}
```

### DatabaseFactory Pattern

The project uses a **DatabaseFactory** pattern to manage database selection:

```csharp
// Utils/DatabaseFactory.cs
public interface IDatabaseFactory
{
    T GetRepository<T>() where T : class;
    DatabaseType GetDefaultDatabaseType();
    bool IsDatabaseEnabled(DatabaseType databaseType);
}
```

## Repository Pattern Usage

### Base Models

#### For SQL Server Entities (BaseDomain)

```csharp
// Data/Models/BaseDomain.cs
public class BaseDomain
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
}
```

#### For MongoDB Documents (BaseMongoDocument)

```csharp
// Data/Models/BaseMongoDocument.cs
public class BaseMongoDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.Binary)]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime? UpdatedAt { get; set; }

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; } = false;
}
```

### Repository Interfaces

#### SQL Server Repository Interface

```csharp
// Data/Repos/Interfaces/ISqlRepository.cs
public interface ISqlRepository<T> where T : BaseDomain
{
    Task<IEnumerable<T>> FindAllAsync();
    Task<IEnumerable<T>> FindAllAsync(params Expression<Func<T, object>>[] includes);
    Task<T?> FindAsync(Guid id);
    Task<T> AddAsync(T entity);
    Task<T?> UpdateAsync(T entity);
    Task<T?> DeleteAsync(T entity);
    Task<IEnumerable<T>> FindByConditionAsync(Expression<Func<T, bool>> expression);
    Task<IEnumerable<T>> FindByConditionAsync(Expression<Func<T, bool>> expression, params Expression<Func<T, object>>[] includes);
    Task<int> GetCountAsync();
    Task<bool> ExistsAsync(Guid id);
}
```

#### MongoDB Repository Interface

```csharp
// Data/Repos/Interfaces/IMongoRepository.cs
public interface IMongoRepository<T> where T : BaseMongoDocument
{
    Task<IEnumerable<T>> FindAllAsync();
    Task<T?> FindAsync(Guid id);
    Task<T> AddAsync(T document);
    Task<T?> UpdateAsync(T document);
    Task<T?> DeleteAsync(Guid id);
    Task<IEnumerable<T>> FindByConditionAsync(Expression<Func<T, bool>> expression);
    Task<IEnumerable<T>> FindByFilterAsync(FilterDefinition<T> filter);
    Task<IEnumerable<T>> FindByFilterAsync(FilterDefinition<T> filter, SortDefinition<T> sort);
    Task<IEnumerable<T>> FindByFilterAsync(FilterDefinition<T> filter, int skip, int limit);
    Task<long> GetCountAsync();
    Task<bool> ExistsAsync(Guid id);
    Task<T?> FindOneAndUpdateAsync(FilterDefinition<T> filter, UpdateDefinition<T> update);
    Task<T?> FindOneAndDeleteAsync(FilterDefinition<T> filter);
}
```

### Repository Usage Examples

#### Using DatabaseFactory with SQL Server

```csharp
// In a service
public class StudentService
{
    private readonly IDatabaseFactory _databaseFactory;
    private readonly ILogger<StudentService> _logger;

    public StudentService(IDatabaseFactory databaseFactory, ILogger<StudentService> logger)
    {
        _databaseFactory = databaseFactory;
        _logger = logger;
    }

    // Get all students
    public async Task<IEnumerable<Student>> GetAllStudentsAsync()
    {
        var repository = _databaseFactory.GetRepository<ISqlRepository<Student>>();
        return await repository.FindAllAsync();
    }

    // Get student by ID
    public async Task<Student?> GetStudentByIdAsync(Guid id)
    {
        var repository = _databaseFactory.GetRepository<ISqlRepository<Student>>();
        return await repository.FindAsync(id);
    }

    // Create new student
    public async Task<Student> CreateStudentAsync(Student student)
    {
        var repository = _databaseFactory.GetRepository<ISqlRepository<Student>>();
        return await repository.AddAsync(student);
    }

    // Update student
    public async Task<Student?> UpdateStudentAsync(Student student)
    {
        var repository = _databaseFactory.GetRepository<ISqlRepository<Student>>();
        return await repository.UpdateAsync(student);
    }

    // Soft delete student
    public async Task<Student?> DeleteStudentAsync(Student student)
    {
        var repository = _databaseFactory.GetRepository<ISqlRepository<Student>>();
        return await repository.DeleteAsync(student);
    }

    // Find students by condition
    public async Task<IEnumerable<Student>> FindStudentsByParentAsync(Guid parentId)
    {
        var repository = _databaseFactory.GetRepository<ISqlRepository<Student>>();
        return await repository.FindByConditionAsync(s => s.ParentId == parentId);
    }

    // Check if student exists
    public async Task<bool> StudentExistsAsync(Guid id)
    {
        var repository = _databaseFactory.GetRepository<ISqlRepository<Student>>();
        return await repository.ExistsAsync(id);
    }
}
```

#### Using DatabaseFactory with MongoDB

```csharp
// In a service
public class NotificationService
{
    private readonly IDatabaseFactory _databaseFactory;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IDatabaseFactory databaseFactory, ILogger<NotificationService> logger)
    {
        _databaseFactory = databaseFactory;
        _logger = logger;
    }

    // Get all notifications
    public async Task<IEnumerable<Notification>> GetAllNotificationsAsync()
    {
        var repository = _databaseFactory.GetRepositoryByType<IMongoRepository<Notification>>(DatabaseType.MongoDb);
        return await repository.FindAllAsync();
    }

    // Get notification by ID
    public async Task<Notification?> GetNotificationByIdAsync(Guid id)
    {
        var repository = _databaseFactory.GetRepositoryByType<IMongoRepository<Notification>>(DatabaseType.MongoDb);
        return await repository.FindAsync(id);
    }

    // Create new notification
    public async Task<Notification> CreateNotificationAsync(Notification notification)
    {
        var repository = _databaseFactory.GetRepositoryByType<IMongoRepository<Notification>>(DatabaseType.MongoDb);
        return await repository.AddAsync(notification);
    }

    // Update notification
    public async Task<Notification?> UpdateNotificationAsync(Notification notification)
    {
        var repository = _databaseFactory.GetRepositoryByType<IMongoRepository<Notification>>(DatabaseType.MongoDb);
        return await repository.UpdateAsync(notification);
    }

    // Soft delete notification
    public async Task<Notification?> DeleteNotificationAsync(Guid id)
    {
        var repository = _databaseFactory.GetRepositoryByType<IMongoRepository<Notification>>(DatabaseType.MongoDb);
        return await repository.DeleteAsync(id);
    }

    // Find notifications by condition
    public async Task<IEnumerable<Notification>> FindNotificationsByUserIdAsync(Guid userId)
    {
        var repository = _databaseFactory.GetRepositoryByType<IMongoRepository<Notification>>(DatabaseType.MongoDb);
        return await repository.FindByConditionAsync(n => n.UserId == userId);
    }

    // Advanced MongoDB filtering
    public async Task<IEnumerable<Notification>> FindNotificationsByTypeAsync(string notificationType)
    {
        var repository = _databaseFactory.GetRepositoryByType<IMongoRepository<Notification>>(DatabaseType.MongoDb);
        var filter = Builders<Notification>.Filter.Eq(n => n.NotificationType, notificationType);
        return await repository.FindByFilterAsync(filter);
    }
}
```

## Creating Domain Models

### SQL Server Entity Example

```csharp
// Data/Models/Student.cs
namespace Data.Models;

public partial class Student : BaseDomain
{
    public Guid ParentId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public bool IsActive { get; set; }

    // Navigation properties for relationships
    public virtual ICollection<Image> Images { get; set; } = new List<Image>();
    public virtual Parent Parent { get; set; } = null!;
    public virtual ICollection<StudentGradeEnrollment> StudentGradeEnrollments { get; set; } = new List<StudentGradeEnrollment>();
    public virtual StudentPickupPoint? StudentPickupPoint { get; set; }
    public virtual ICollection<TransportFeeItem> TransportFeeItems { get; set; } = new List<TransportFeeItem>();
}
```

### MongoDB Document Example

```csharp
// Data/Models/Notification.cs
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Data.Models
{
    public class Notification : BaseMongoDocument
    {
        [BsonElement("userId")]
        public Guid UserId { get; set; }

        [BsonElement("message")]
        public string Message { get; set; } = string.Empty;

        [BsonElement("notificationType")]
        public string NotificationType { get; set; } = string.Empty;

        [BsonElement("recipientType")]
        public string RecipientType { get; set; } = string.Empty;

        [BsonElement("status")]
        public string Status { get; set; } = string.Empty;

        [BsonElement("timeStamp")]
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
    }
}
```

## Implementing Services

### Service Interface

```csharp
// Services/Contracts/IStudentService.cs
public interface IStudentService
{
    Task<IEnumerable<Student>> GetAllStudentsAsync();
    Task<Student?> GetStudentByIdAsync(Guid id);
    Task<Student> CreateStudentAsync(Student student);
    Task<Student?> UpdateStudentAsync(Student student);
    Task<Student?> DeleteStudentAsync(Student student);
    Task<IEnumerable<Student>> GetActiveStudentsAsync();
    Task<bool> StudentExistsAsync(Guid id);
    Task<IEnumerable<Student>> FindStudentsByParentAsync(Guid parentId);
}
```

### Service Implementation

```csharp
// Services/Implementations/StudentService.cs
using Data.Repos.Interfaces;
using Services.Contracts;
using Utils;

public class StudentService : IStudentService
{
    private readonly IDatabaseFactory _databaseFactory;
    private readonly ILogger<StudentService> _logger;

    public StudentService(IDatabaseFactory databaseFactory, ILogger<StudentService> logger)
    {
        _databaseFactory = databaseFactory;
        _logger = logger;
    }

    public async Task<IEnumerable<Student>> GetAllStudentsAsync()
    {
        try
        {
            var repository = _databaseFactory.GetRepository<ISqlRepository<Student>>();
            return await repository.FindAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all students");
            throw;
        }
    }

    public async Task<Student?> GetStudentByIdAsync(Guid id)
    {
        try
        {
            var repository = _databaseFactory.GetRepository<ISqlRepository<Student>>();
            return await repository.FindAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting student with id: {StudentId}", id);
            throw;
        }
    }

    public async Task<Student> CreateStudentAsync(Student student)
    {
        try
        {
            // Add business logic validation
            if (string.IsNullOrWhiteSpace(student.FirstName))
            {
                throw new ArgumentException("First name is required");
            }

            if (string.IsNullOrWhiteSpace(student.LastName))
            {
                throw new ArgumentException("Last name is required");
            }

            var repository = _databaseFactory.GetRepository<ISqlRepository<Student>>();
            return await repository.AddAsync(student);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating student: {@Student}", student);
            throw;
        }
    }

    public async Task<Student?> UpdateStudentAsync(Student student)
    {
        try
        {
            var repository = _databaseFactory.GetRepository<ISqlRepository<Student>>();
            var existingStudent = await repository.FindAsync(student.Id);
            if (existingStudent == null)
            {
                return null;
            }

            // Add business logic validation
            if (string.IsNullOrWhiteSpace(student.FirstName))
            {
                throw new ArgumentException("First name is required");
            }

            return await repository.UpdateAsync(student);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating student: {@Student}", student);
            throw;
        }
    }

    public async Task<Student?> DeleteStudentAsync(Student student)
    {
        try
        {
            var repository = _databaseFactory.GetRepository<ISqlRepository<Student>>();
            return await repository.DeleteAsync(student);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting student: {@Student}", student);
            throw;
        }
    }

    public async Task<IEnumerable<Student>> GetActiveStudentsAsync()
    {
        try
        {
            var repository = _databaseFactory.GetRepository<ISqlRepository<Student>>();
            return await repository.FindByConditionAsync(s => s.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active students");
            throw;
        }
    }

    public async Task<bool> StudentExistsAsync(Guid id)
    {
        try
        {
            var repository = _databaseFactory.GetRepository<ISqlRepository<Student>>();
            return await repository.ExistsAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if student exists: {StudentId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<Student>> FindStudentsByParentAsync(Guid parentId)
    {
        try
        {
            var repository = _databaseFactory.GetRepository<ISqlRepository<Student>>();
            return await repository.FindByConditionAsync(s => s.ParentId == parentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding students by parent: {ParentId}", parentId);
            throw;
        }
    }
}
```

## Creating API Controllers

### Controller Example

```csharp
// APIs/Controllers/StudentsController.cs
using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Data.Models;

[ApiController]
[Route("api/[controller]")]
public class StudentsController : ControllerBase
{
    private readonly IStudentService _studentService;
    private readonly ILogger<StudentsController> _logger;

    public StudentsController(IStudentService studentService, ILogger<StudentsController> logger)
    {
        _studentService = studentService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Student>>> GetStudents()
    {
        try
        {
            var students = await _studentService.GetAllStudentsAsync();
            return Ok(students);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting students");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Student>> GetStudent(Guid id)
    {
        try
        {
            var student = await _studentService.GetStudentByIdAsync(id);
            if (student == null)
            {
                return NotFound(new { message = "Student not found" });
            }

            return Ok(student);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting student with id: {StudentId}", id);
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<Student>> CreateStudent(Student student)
    {
        try
        {
            var createdStudent = await _studentService.CreateStudentAsync(student);
            return CreatedAtAction(nameof(GetStudent), new { id = createdStudent.Id }, createdStudent);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating student");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateStudent(Guid id, Student student)
    {
        try
        {
            if (id != student.Id)
            {
                return BadRequest(new { message = "ID mismatch" });
            }

            var updatedStudent = await _studentService.UpdateStudentAsync(student);
            if (updatedStudent == null)
            {
                return NotFound(new { message = "Student not found" });
            }

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating student");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteStudent(Guid id)
    {
        try
        {
            var student = await _studentService.GetStudentByIdAsync(id);
            if (student == null)
            {
                return NotFound(new { message = "Student not found" });
            }

            var deletedStudent = await _studentService.DeleteStudentAsync(student);
            if (deletedStudent == null)
            {
                return NotFound(new { message = "Student not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting student");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<Student>>> GetActiveStudents()
    {
        try
        {
            var students = await _studentService.GetActiveStudentsAsync();
            return Ok(students);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active students");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    [HttpGet("parent/{parentId}")]
    public async Task<ActionResult<IEnumerable<Student>>> GetStudentsByParent(Guid parentId)
    {
        try
        {
            var students = await _studentService.FindStudentsByParentAsync(parentId);
            return Ok(students);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting students by parent");
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
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IParentService, ParentService>();
```

### Environment-Specific Configuration

#### Development Environment (User Secrets)

For local development, use User Secrets to store sensitive configuration:

```bash
# Initialize User Secrets (if not already done)
cd APIs
dotnet user-secrets init

# Add configuration
dotnet user-secrets set "ConnectionStrings:SqlServer" "Server=LAPTOP-DVKPB8S9;Database=edubus_dev;Trusted_Connection=True;Encrypt=false"
dotnet user-secrets set "ConnectionStrings:MongoDb" "mongodb://localhost:27017/edubus"
dotnet user-secrets set "DatabaseSettings:DefaultDatabase" "SqlServer"
dotnet user-secrets set "DatabaseSettings:UseMultipleDatabases" "true"
```

#### Development Environment (appsettings.Development.json)

Create `appsettings.Development.json` for non-sensitive configuration:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "MongoDb": {
    "DatabaseName": "EduBusDB_Dev"
  }
}
```

#### Production Environment

For production, use environment variables or secure configuration providers:

```bash
# Environment Variables
export ConnectionStrings__SqlServer="Server=prod-server;Database=EduBus;User Id=app_user;Password=secure_password"
export ConnectionStrings__MongoDb="mongodb://prod-mongo:27017"
export DatabaseSettings__DefaultDatabase="SqlServer"
export DatabaseSettings__UseMultipleDatabases="true"
```

#### Configuration Priority

1. **User Secrets** (Development only)
2. **Environment Variables**
3. **appsettings.{Environment}.json**
4. **appsettings.json**

## Best Practices

### 1. Security Best Practices

#### User Secrets Management

- ✅ **Use User Secrets for local development**
- ✅ **Never commit sensitive data to source control**
- ✅ **Use environment variables in production**
- ✅ **Rotate secrets regularly**

```bash
# ✅ Good - Using User Secrets
dotnet user-secrets set "ConnectionStrings:SqlServer" "Server=LAPTOP-DVKPB8S9;Database=edubus_dev;Trusted_Connection=True;Encrypt=false"

# ❌ Bad - Hard-coded in appsettings.json
{
  "ConnectionStrings": {
    "SqlServer": "Server=LAPTOP-DVKPB8S9;Database=edubus_dev;Trusted_Connection=True;Encrypt=false"
  }
}
```

#### Connection String Security

- Use strong passwords
- Limit database user permissions
- Use connection pooling
- Enable SSL/TLS for production

### 2. Naming Conventions

- **Classes**: PascalCase (e.g., `StudentService`, `StudentsController`)
- **Methods**: PascalCase (e.g., `GetAllStudentsAsync`, `CreateStudentAsync`)
- **Properties**: PascalCase (e.g., `FirstName`, `LastName`)
- **Private fields**: camelCase with underscore (e.g., `_studentService`)
- **Constants**: UPPER_CASE (e.g., `MAX_CAPACITY`)

### 2. Async/Await Pattern

Always use async/await for I/O operations:

```csharp
// ✅ Good
public async Task<IEnumerable<Student>> GetAllStudentsAsync()
{
    var repository = _databaseFactory.GetRepository<ISqlRepository<Student>>();
    return await repository.FindAllAsync();
}

// ❌ Bad
public IEnumerable<Student> GetAllStudents()
{
    var repository = _databaseFactory.GetRepository<ISqlRepository<Student>>();
    return repository.FindAllAsync().Result;
}
```

### 3. Error Handling

```csharp
public async Task<Student?> GetStudentByIdAsync(Guid id)
{
    try
    {
        var repository = _databaseFactory.GetRepository<ISqlRepository<Student>>();
        return await repository.FindAsync(id);
    }
    catch (Exception ex)
    {
        // Log the exception
        _logger.LogError(ex, "Error retrieving student with ID {StudentId}", id);
        throw;
    }
}
```

### 4. Validation

```csharp
public async Task<Student> CreateStudentAsync(Student student)
{
    // Input validation
    if (student == null)
        throw new ArgumentNullException(nameof(student));

    if (string.IsNullOrWhiteSpace(student.FirstName))
        throw new ArgumentException("First name is required", nameof(student));

    if (string.IsNullOrWhiteSpace(student.LastName))
        throw new ArgumentException("Last name is required", nameof(student));

    // Business logic validation
    var repository = _databaseFactory.GetRepository<ISqlRepository<Student>>();
    var existingStudents = await repository.FindByConditionAsync(s =>
        s.FirstName == student.FirstName &&
        s.LastName == student.LastName &&
        s.ParentId == student.ParentId);

    if (existingStudents.Any())
        throw new InvalidOperationException("Student with same name and parent already exists");

    return await repository.AddAsync(student);
}
```

### 5. DatabaseFactory Usage

Choose the appropriate repository based on your entity type:

```csharp
// For SQL Server entities (inherit from BaseDomain)
var repository = _databaseFactory.GetRepository<ISqlRepository<Student>>();

// For MongoDB documents (inherit from BaseMongoDocument)
var repository = _databaseFactory.GetRepositoryByType<IMongoRepository<Notification>>(DatabaseType.MongoDb);
```

### 6. ID Type Consistency

The project uses **Guid** for all IDs:

```csharp
// ✅ Correct - Using Guid
public Guid Id { get; set; } = Guid.NewGuid();

// ❌ Incorrect - Using int
public int Id { get; set; }
```

## Common Patterns

### 1. Service Layer Pattern with DatabaseFactory

```csharp
// Interface
public interface IEntityService<T> where T : BaseDomain
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<T?> GetByIdAsync(Guid id);
    Task<T> CreateAsync(T entity);
    Task<T?> UpdateAsync(T entity);
    Task<T?> DeleteAsync(T entity);
}

// Implementation
public abstract class BaseService<T> : IEntityService<T> where T : BaseDomain
{
    protected readonly IDatabaseFactory _databaseFactory;
    protected readonly ILogger<BaseService<T>> _logger;

    protected BaseService(IDatabaseFactory databaseFactory, ILogger<BaseService<T>> logger)
    {
        _databaseFactory = databaseFactory;
        _logger = logger;
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        var repository = _databaseFactory.GetRepository<ISqlRepository<T>>();
        return await repository.FindAllAsync();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        var repository = _databaseFactory.GetRepository<ISqlRepository<T>>();
        return await repository.FindAsync(id);
    }

    public virtual async Task<T> CreateAsync(T entity)
    {
        var repository = _databaseFactory.GetRepository<ISqlRepository<T>>();
        return await repository.AddAsync(entity);
    }

    public virtual async Task<T?> UpdateAsync(T entity)
    {
        var repository = _databaseFactory.GetRepository<ISqlRepository<T>>();
        return await repository.UpdateAsync(entity);
    }

    public virtual async Task<T?> DeleteAsync(T entity)
    {
        var repository = _databaseFactory.GetRepository<ISqlRepository<T>>();
        return await repository.DeleteAsync(entity);
    }
}
```

### 2. Generic Controller Pattern

```csharp
public abstract class BaseController<T> : ControllerBase where T : BaseDomain
{
    protected readonly IEntityService<T> _service;
    protected readonly ILogger<BaseController<T>> _logger;

    protected BaseController(IEntityService<T> service, ILogger<BaseController<T>> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public virtual async Task<ActionResult<IEnumerable<T>>> GetAll()
    {
        try
        {
            var entities = await _service.GetAllAsync();
            return Ok(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all entities");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{id}")]
    public virtual async Task<ActionResult<T>> GetById(Guid id)
    {
        try
        {
            var entity = await _service.GetByIdAsync(id);
            if (entity == null)
                return NotFound(new { message = "Entity not found" });

            return Ok(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entity by id");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost]
    public virtual async Task<ActionResult<T>> Create(T entity)
    {
        try
        {
            var created = await _service.CreateAsync(entity);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating entity");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{id}")]
    public virtual async Task<IActionResult> Update(Guid id, T entity)
    {
        try
        {
            if (id != entity.Id)
                return BadRequest(new { message = "ID mismatch" });

            var updated = await _service.UpdateAsync(entity);
            if (updated == null)
                return NotFound(new { message = "Entity not found" });

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating entity");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("{id}")]
    public virtual async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var entity = await _service.GetByIdAsync(id);
            if (entity == null)
                return NotFound(new { message = "Entity not found" });

            var deleted = await _service.DeleteAsync(entity);
            if (deleted == null)
                return NotFound(new { message = "Entity not found" });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting entity");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
```

## Troubleshooting

### Common Issues

1. **Repository not found in DI container**

   - Ensure repository is registered in `Program.cs`
   - Check if using correct repository interface
   - Verify DatabaseFactory is properly configured

2. **MongoDB connection issues**

   - Verify MongoDB is running
   - Check connection string in User Secrets or `appsettings.json`
   - Ensure database name is configured
   - Verify MongoDB service is accessible

3. **SQL Server connection issues**

   - Verify SQL Server is running
   - Check connection string in User Secrets or `appsettings.json`
   - Ensure database exists
   - Verify SQL Server authentication settings

4. **User Secrets issues**

   - Ensure User Secrets are initialized: `dotnet user-secrets init`
   - Check if running in Development environment
   - Verify User Secrets are properly set: `dotnet user-secrets list`
   - Ensure User Secrets are not corrupted

5. **Entity Framework issues**

   - Add migrations: `dotnet ef migrations add InitialCreate`
   - Update database: `dotnet ef database update`
   - Check connection string in User Secrets

6. **DatabaseFactory issues**
   - Check configuration in User Secrets or `appsettings.json`
   - Verify service registration in `Program.cs`
   - Ensure correct database type is specified
   - Check if both databases are properly configured

### Debugging Tips

1. **Enable detailed logging**
2. **Use Swagger UI for API testing**
3. **Check database connections**
4. **Verify dependency injection setup**
5. **Use health checks to verify database connectivity**

## Next Steps

1. Implement business logic services for all domain models
2. Create API controllers for all entities
3. Add authentication and authorization
4. Implement validation and error handling
5. Add unit tests
6. Configure logging and monitoring
7. Add API documentation
8. Implement caching strategies

This guide provides a foundation for developing with the EduBus APIs architecture. Follow the patterns and best practices outlined to ensure consistent, maintainable code.
