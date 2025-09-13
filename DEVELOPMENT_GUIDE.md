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
9. [Payment System Setup (PayOS)](#payment-system-setup-payos)
10. [Best Practices](#best-practices)
11. [Common Patterns](#common-patterns)
12. [Troubleshooting](#troubleshooting)

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

**Step 3: Add PayOS Configuration**

```bash
# PayOS Payment Gateway Configuration
dotnet user-secrets set "PayOS:ClientId" "your-payos-client-id"
dotnet user-secrets set "PayOS:ApiKey" "your-payos-api-key"
dotnet user-secrets set "PayOS:ChecksumKey" "your-payos-checksum-key"
dotnet user-secrets set "PayOS:BaseUrl" "https://api-merchant.payos.vn"
dotnet user-secrets set "PayOS:WebhookUrl" "https://your-domain.com/api/payment/webhook/payos"
dotnet user-secrets set "PayOS:ReturnUrl" "https://localhost:7000/api/payment/return"
dotnet user-secrets set "PayOS:CancelUrl" "https://localhost:7000/api/payment/cancel"
dotnet user-secrets set "PayOS:QrExpirationMinutes" "15"
```

**Step 4: Verify User Secrets**

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
  },
  "PayOS": {
    "ClientId": "your-payos-client-id",
    "ApiKey": "your-payos-api-key",
    "ChecksumKey": "your-payos-checksum-key",
    "BaseUrl": "https://api-merchant.payos.vn",
    "WebhookUrl": "https://your-domain.com/api/payment/webhook/payos",
    "ReturnUrl": "https://localhost:7000/api/payment/return",
    "CancelUrl": "https://localhost:7000/api/payment/cancel",
    "QrExpirationMinutes": 15
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

# For different PayOS configuration
dotnet user-secrets set "PayOS:ClientId" "YOUR_PAYOS_CLIENT_ID"
dotnet user-secrets set "PayOS:ApiKey" "YOUR_PAYOS_API_KEY"
dotnet user-secrets set "PayOS:ChecksumKey" "YOUR_PAYOS_CHECKSUM_KEY"
dotnet user-secrets set "PayOS:WebhookUrl" "https://YOUR_DOMAIN.com/api/payment/webhook/payos"
dotnet user-secrets set "PayOS:ReturnUrl" "https://YOUR_DOMAIN.com/api/payment/return"
dotnet user-secrets set "PayOS:CancelUrl" "https://YOUR_DOMAIN.com/api/payment/cancel"
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

// Payment Services
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPayOSService, PayOSService>();

// PayOS Configuration
builder.Services.Configure<Services.Models.Payment.PayOSConfig>(
    builder.Configuration.GetSection("PayOS"));

// PayOS HttpClient
builder.Services.AddHttpClient<IPayOSService, PayOSService>();
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
export PayOS__ClientId="prod-payos-client-id"
export PayOS__ApiKey="prod-payos-api-key"
export PayOS__ChecksumKey="prod-payos-checksum-key"
export PayOS__WebhookUrl="https://prod-domain.com/api/payment/webhook/payos"
export PayOS__ReturnUrl="https://prod-domain.com/api/payment/return"
export PayOS__CancelUrl="https://prod-domain.com/api/payment/cancel"
```

#### Configuration Priority

1. **User Secrets** (Development only)
2. **Environment Variables**
3. **appsettings.{Environment}.json**
4. **appsettings.json**

## Payment System Setup (PayOS)

### Overview

The EduBus system integrates with PayOS payment gateway to handle payment transactions for pickup point services. This section covers the complete setup and configuration process.

### PayOS Account Setup

1. **Create PayOS Account**

   - Visit [PayOS Developer Portal](https://dev.payos.vn/)
   - Register for a developer account
   - Complete merchant verification process

2. **Get API Credentials**
   - Navigate to your PayOS dashboard
   - Go to "API Keys" section
   - Generate or retrieve your credentials:
     - `ClientId`: Your PayOS client identifier
     - `ApiKey`: Your PayOS API key
     - `ChecksumKey`: Your PayOS checksum key for webhook verification

### Configuration Setup

#### Development Environment

**Step 1: Configure PayOS Settings**

```bash
# Set PayOS credentials
dotnet user-secrets set "PayOS:ClientId" "your-actual-client-id"
dotnet user-secrets set "PayOS:ApiKey" "your-actual-api-key"
dotnet user-secrets set "PayOS:ChecksumKey" "your-actual-checksum-key"

# Set PayOS URLs
dotnet user-secrets set "PayOS:BaseUrl" "https://api-merchant.payos.vn"
dotnet user-secrets set "PayOS:WebhookUrl" "https://your-ngrok-url.ngrok.io/api/payment/webhook/payos"
dotnet user-secrets set "PayOS:ReturnUrl" "https://localhost:7000/api/payment/return"
dotnet user-secrets set "PayOS:CancelUrl" "https://localhost:7000/api/payment/cancel"
dotnet user-secrets set "PayOS:QrExpirationMinutes" "15"
```

**Step 2: Setup ngrok for Webhook Testing**

For local development, you need to expose your local server to receive PayOS webhooks:

```bash
# Install ngrok
npm install -g ngrok

# Expose local server
ngrok http 7000

# Use the https URL for webhook configuration
# Example: https://abc123.ngrok.io/api/payment/webhook/payos
```

#### Production Environment

**Environment Variables:**

```bash
export PayOS__ClientId="production-client-id"
export PayOS__ApiKey="production-api-key"
export PayOS__ChecksumKey="production-checksum-key"
export PayOS__BaseUrl="https://api-merchant.payos.vn"
export PayOS__WebhookUrl="https://yourdomain.com/api/payment/webhook/payos"
export PayOS__ReturnUrl="https://yourdomain.com/api/payment/return"
export PayOS__CancelUrl="https://yourdomain.com/api/payment/cancel"
export PayOS__QrExpirationMinutes="15"
```

### PayOS Configuration Model

The system uses a configuration model to manage PayOS settings:

```csharp
// Services/Models/Payment/PayOSConfig.cs
namespace Services.Models.Payment;

public class PayOSConfig
{
    public string ClientId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ChecksumKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api-merchant.payos.vn";
    public string WebhookUrl { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
    public int QrExpirationMinutes { get; set; } = 15;
}
```

### Service Registration

Add PayOS services to your `Program.cs`:

```csharp
// Payment Services
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPayOSService, PayOSService>();

// PayOS Configuration
builder.Services.Configure<Services.Models.Payment.PayOSConfig>(
    builder.Configuration.GetSection("PayOS"));

// PayOS HttpClient
builder.Services.AddHttpClient<IPayOSService, PayOSService>();
```

### Payment Flow Implementation

#### 1. Transaction Creation

When a pickup point request is approved, a transaction is automatically created:

```csharp
// In PaymentService
public async Task<Transaction> CreateTransactionForPickupPointAsync(
    string pickupPointRequestId,
    Guid scheduleId)
{
    // Create transaction with PayOS provider
    var transaction = new Transaction
    {
        ParentId = pickupPointRequest.ParentId ?? Guid.Empty,
        TransactionCode = $"TXN_{DateTime.UtcNow:yyyyMMddHHmmss}_{pickupPointRequestId}",
        Status = TransactionStatus.Notyet,
        Amount = totalAmount,
        Currency = "VND",
        Description = $"Phí vận chuyển học sinh - Yêu cầu điểm đón {pickupPointRequestId}",
        Provider = PaymentProvider.PayOS,
        PickupPointRequestId = pickupPointRequestId,
        ScheduleId = scheduleId,
        Metadata = JsonSerializer.Serialize(metadata)
    };

    return await _transactionRepository.AddAsync(transaction);
}
```

#### 2. QR Code Generation

Generate PayOS payment request and QR code:

```csharp
// In PaymentService
public async Task<QrResponse> GenerateOrRefreshQrAsync(Guid transactionId)
{
    var transaction = await _transactionRepository.FindAsync(transactionId);

    // Create PayOS payment request
    var payOSRequest = new PayOSCreatePaymentRequest
    {
        OrderCode = long.Parse(transaction.TransactionCode.Replace("TXN_", "").Replace("_", "")),
        Amount = (int)transaction.Amount,
        Description = transaction.Description,
        Items = transportFeeItems.Select(item => new PayOSItem
        {
            Name = item.Description,
            Quantity = 1,
            Price = (int)item.Subtotal
        }).ToList(),
        ReturnUrl = _payOSConfig.ReturnUrl,
        CancelUrl = _payOSConfig.CancelUrl
    };

    var payOSResponse = await _payOSService.CreatePaymentAsync(payOSRequest);

    return new QrResponse
    {
        QrCode = payOSResponse.Data.QrCode,
        CheckoutUrl = payOSResponse.Data.CheckoutUrl,
        ExpiresAt = DateTime.UtcNow.AddMinutes(_payOSConfig.QrExpirationMinutes)
    };
}
```

#### 3. Webhook Handling

Process PayOS webhook notifications:

```csharp
// In PaymentController
[HttpPost("webhook/payos")]
[AllowAnonymous]
public async Task<IActionResult> HandlePayOSWebhook([FromBody] PayOSWebhookPayload payload)
{
    try
    {
        var success = await _paymentService.HandlePayOSWebhookAsync(payload);

        if (success)
        {
            return Ok(new { success = true, message = "Webhook processed successfully" });
        }
        else
        {
            return BadRequest(new { success = false, message = "Webhook processing failed" });
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing PayOS webhook");
        return StatusCode(500, new { success = false, message = "Internal server error" });
    }
}
```

#### 4. Return/Cancel URL Handling

Handle user redirects from PayOS:

```csharp
// Return URL - Payment Success
[HttpGet("return")]
[AllowAnonymous]
public async Task<IActionResult> HandlePaymentReturn(
    [FromQuery] string code,
    [FromQuery] string id,
    [FromQuery] bool cancel,
    [FromQuery] string status,
    [FromQuery] long orderCode)
{
    var message = cancel ? "Payment was cancelled" : "Payment completed successfully";

    return Ok(new
    {
        success = !cancel,
        message = message,
        orderCode = orderCode.ToString(),
        status = status
    });
}

// Cancel URL - Payment Cancelled
[HttpGet("cancel")]
[AllowAnonymous]
public async Task<IActionResult> HandlePaymentCancel(
    [FromQuery] string code,
    [FromQuery] string id,
    [FromQuery] bool cancel,
    [FromQuery] string status,
    [FromQuery] long orderCode)
{
    return Ok(new
    {
        success = false,
        message = "Payment was cancelled",
        orderCode = orderCode.ToString(),
        status = status
    });
}
```

### Testing PayOS Integration

#### 1. Test Payment Flow

```bash
# 1. Create a transaction
POST /api/payment
{
  "pickupPointRequestId": "request-id",
  "scheduleId": "schedule-id"
}

# 2. Generate QR code
POST /api/payment/{transactionId}/qrcode

# 3. Test webhook (simulate PayOS callback)
POST /api/payment/webhook/payos
{
  "code": "00",
  "desc": "success",
  "success": true,
  "data": {
    "orderCode": 123456789,
    "amount": 150000,
    "description": "Test payment",
    "accountNumber": "1234567890",
    "reference": "TXN_20240115_001",
    "transactionDateTime": "2024-01-15T11:00:00Z",
    "currency": "VND",
    "paymentLinkId": "pay_123456789",
    "code": "00",
    "desc": "success"
  },
  "signature": "valid-signature"
}
```

#### 2. Test Return/Cancel URLs

```bash
# Test return URL
GET /api/payment/return?code=00&id=123456789&cancel=false&status=success&orderCode=123456789

# Test cancel URL
GET /api/payment/cancel?code=01&id=123456789&cancel=true&status=cancelled&orderCode=123456789
```

### Security Considerations

#### 1. Webhook Signature Verification

Always verify PayOS webhook signatures:

```csharp
public async Task<bool> VerifyWebhookSignatureAsync(string signature, string payload)
{
    var expectedSignature = await GenerateChecksumAsync(payload);
    return signature == expectedSignature;
}

private async Task<string> GenerateChecksumAsync(string data)
{
    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_payOSConfig.ChecksumKey));
    var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    return Convert.ToHexString(hashBytes).ToLower();
}
```

#### 2. Environment-Specific URLs

- **Development**: Use ngrok URLs for webhooks
- **Production**: Use your actual domain URLs
- **Staging**: Use staging domain URLs

#### 3. API Key Security

- Store PayOS credentials in User Secrets (development) or Environment Variables (production)
- Never commit API keys to source control
- Rotate API keys regularly
- Use different keys for different environments

### Troubleshooting PayOS Integration

#### Common Issues

1. **Webhook Not Received**

   - Check ngrok is running and URL is correct
   - Verify webhook URL in PayOS dashboard
   - Check firewall settings

2. **Signature Verification Failed**

   - Verify checksum key is correct
   - Check payload encoding (UTF-8)
   - Ensure signature comparison is case-sensitive

3. **Payment Request Creation Failed**

   - Verify API credentials
   - Check PayOS API status
   - Validate request payload format

4. **Return/Cancel URLs Not Working**
   - Verify URLs are accessible
   - Check CORS settings
   - Validate query parameter parsing

#### Debug Tips

1. **Enable Detailed Logging**

   ```csharp
   _logger.LogInformation("PayOS Request: {@Request}", payOSRequest);
   _logger.LogInformation("PayOS Response: {@Response}", payOSResponse);
   ```

2. **Test with PayOS Sandbox**

   - Use PayOS sandbox environment for testing
   - Test with different payment scenarios
   - Verify webhook delivery

3. **Monitor PayOS Dashboard**
   - Check transaction status in PayOS dashboard
   - Monitor webhook delivery logs
   - Review API usage statistics

### Production Deployment

#### 1. Environment Configuration

```bash
# Production environment variables
export PayOS__ClientId="prod-client-id"
export PayOS__ApiKey="prod-api-key"
export PayOS__ChecksumKey="prod-checksum-key"
export PayOS__WebhookUrl="https://api.yourdomain.com/api/payment/webhook/payos"
export PayOS__ReturnUrl="https://app.yourdomain.com/payment/success"
export PayOS__CancelUrl="https://app.yourdomain.com/payment/cancel"
```

#### 2. SSL Certificate

- Ensure all URLs use HTTPS
- Configure SSL certificates for your domain
- Update PayOS webhook URL to use HTTPS

#### 3. Monitoring

- Set up monitoring for payment transactions
- Monitor webhook delivery success rates
- Track payment completion rates
- Set up alerts for failed payments

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

7. **PayOS Integration issues**
   - Verify PayOS credentials in User Secrets or Environment Variables
   - Check PayOS API status and connectivity
   - Ensure webhook URLs are accessible (use ngrok for local development)
   - Verify signature verification logic
   - Check PayOS dashboard for transaction status

### Debugging Tips

1. **Enable detailed logging**
2. **Use Swagger UI for API testing**
3. **Check database connections**
4. **Verify dependency injection setup**
5. **Use health checks to verify database connectivity**
6. **Test PayOS integration with sandbox environment**
7. **Use ngrok for local webhook testing**
8. **Monitor PayOS dashboard for transaction status**
9. **Check webhook signature verification**
10. **Verify PayOS API credentials and connectivity**

## Next Steps

1. Implement business logic services for all domain models
2. Create API controllers for all entities
3. Add authentication and authorization
4. Implement validation and error handling
5. Add unit tests
6. Configure logging and monitoring
7. Add API documentation
8. Implement caching strategies
9. **Set up PayOS payment integration**
10. **Configure webhook handling for payment notifications**
11. **Test payment flows in sandbox environment**
12. **Deploy payment system to production**

This guide provides a foundation for developing with the EduBus APIs architecture. Follow the patterns and best practices outlined to ensure consistent, maintainable code.

### Payment System Quick Start

For developers focusing on payment functionality:

1. **Setup PayOS Account**: Register at [PayOS Developer Portal](https://dev.payos.vn/)
2. **Configure Credentials**: Add PayOS credentials to User Secrets
3. **Setup ngrok**: Install and configure ngrok for webhook testing
4. **Test Integration**: Use the provided test scenarios to verify payment flow
5. **Deploy**: Configure production environment variables for PayOS

The payment system is fully integrated and ready for testing and production deployment.
