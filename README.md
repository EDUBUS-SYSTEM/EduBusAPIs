# EduBus APIs

A comprehensive backend API system for educational bus management, built with .NET 8.0 and following clean architecture principles with dual database support.

## 📋 Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Technology Stack](#technology-stack)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
- [Development Status](#development-status)
- [API Documentation](#api-documentation)
- [Contributing](#contributing)
- [Documentation](#documentation)

## 🚌 Overview

EduBus APIs is a backend system designed to manage educational bus services, including:

- Bus route management
- Schedule coordination
- Booking and reservation system
- Payment processing
- Real-time tracking
- User management (admin, driver, parent, student)

The system is built using modern .NET 8.0 technologies with **dual database support** (SQL Server + MongoDB) and follows clean architecture principles for maintainability and scalability.

## 🏗️ Architecture

The project follows **Clean Architecture** with clear separation of concerns and **dual database support**:

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                       │
│                    (APIs Project)                          │
├─────────────────────────────────────────────────────────────┤
│                   Business Logic Layer                      │
│                   (Services Project)                       │
├─────────────────────────────────────────────────────────────┤
│                    Data Access Layer                        │
│                    (Data Project)                          │
│              ┌─────────────────────────────┐               │
│              │    SQL Server Repository    │               │
│              │    (Entity Framework)       │               │
│              └─────────────────────────────┘               │
│              ┌─────────────────────────────┐               │
│              │    MongoDB Repository       │               │
│              │    (MongoDB.Driver)         │               │
│              └─────────────────────────────┘               │
├─────────────────────────────────────────────────────────────┤
│                  Infrastructure Layer                       │
│              (Utils & Constants Projects)                  │
└─────────────────────────────────────────────────────────────┘
```

### Architecture Principles

- **Separation of Concerns**: Each layer has a specific responsibility
- **Dependency Inversion**: High-level modules don't depend on low-level modules
- **Repository Pattern**: Abstraction over data access with dual database support
- **Generic Repository**: Reusable data access for all entities
- **Factory Pattern**: Database selection based on configuration
- **Async/Await**: Non-blocking operations throughout
- **Soft Delete**: Data integrity preservation

### Dual Database Architecture

- **SQL Server**: For relational data (users, roles, structured business data)
- **MongoDB**: For document-based data (logs, analytics, flexible schemas)
- **Configuration-based**: Database selection through appsettings.json
- **Repository Abstraction**: Unified interface for both database types

## 🛠️ Technology Stack

### Core Technologies

- **.NET 8.0**: Latest LTS version
- **ASP.NET Core**: Web framework
- **Entity Framework Core**: ORM for SQL Server
- **MongoDB.Driver**: Official MongoDB driver for .NET

### Database Technologies

- **SQL Server**: Relational database with Entity Framework Core
- **MongoDB**: NoSQL database with MongoDB.Driver
- **Dual Database Support**: Configuration-based database selection

### Key Libraries

- **Swashbuckle.AspNetCore**: API documentation
- **Microsoft.Extensions.Configuration**: Configuration management
- **Microsoft.Extensions.DependencyInjection**: Dependency injection

### Development Tools

- **Visual Studio 2022**: Primary IDE
- **Git**: Version control
- **Swagger UI**: API testing and documentation

## 📁 Project Structure

```
EduBusAPIs/
├── 📄 EduBusAPIs.sln              # Solution file
├── 🌐 APIs/                       # Web API Layer
│   ├── Controllers/               # API Controllers (empty)
│   ├── Program.cs                 # Application entry point
│   ├── appsettings.json          # Configuration
│   └── APIs.csproj               # Project file
├── ⚙️ Services/                   # Business Logic Layer
│   ├── Contracts/                # Service interfaces (empty)
│   ├── Implementations/          # Service implementations (empty)
│   ├── Models/                   # Business models/DTOs (empty)
│   └── MapperProfiles/           # AutoMapper profiles (empty)
├── 🗄️ Data/                      # Data Access Layer
│   ├── Models/                   # Domain entities
│   │   ├── BaseDomain.cs         # Base entity class (SQL Server)
│   │   └── BaseMongoDocument.cs  # Base document class (MongoDB)
│   ├── Contexts/                 # Database contexts
│   │   ├── MongoDB/
│   │   │   └── EduBusMongoContext.cs
│   │   └── SqlServer/            # Empty (DbContext to be implemented)
│   └── Repos/                    # Repository implementations
│       ├── Interfaces/
│       │   ├── IMongoRepository.cs
│       │   └── ISqlRepository.cs
│       ├── MongoDB/
│       │   └── MongoRepository.cs
│       └── SqlServer/
│           └── SqlRepository.cs
├── 🔧 Utils/                     # Utility Layer
│   └── DatabaseFactory.cs        # Database factory pattern
└── 📋 Constants/                 # Constants Layer (empty)
```

## 🚀 Getting Started

### Prerequisites

- **.NET 8.0 SDK**: [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Visual Studio 2022** or **VS Code**
- **SQL Server** (LocalDB or full instance)
- **MongoDB** (local installation or cloud)

### Installation

1. **Clone the repository**

   ```bash
   git clone <repository-url>
   cd EduBusAPIs
   ```

2. **Restore dependencies**

   ```bash
   dotnet restore
   ```

3. **Build the solution**

   ```bash
   dotnet build
   ```

4. **Configure databases**

   Update `APIs/appsettings.json` with your database connection strings:

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

5. **Run the application**

   ```bash
   cd APIs
   dotnet run
   ```

6. **Access the API**
   - **API Base URL**: `https://localhost:7223` or `http://localhost:5223`
   - **Swagger UI**: `https://localhost:7223/swagger`

## 📊 Development Status

### ✅ Completed

- [x] Solution structure and project setup
- [x] Clean architecture implementation
- [x] Dual database support (SQL Server + MongoDB)
- [x] Repository pattern with generic implementation for both databases
- [x] Base domain models for both database types
- [x] MongoDB context and connection management
- [x] Generic repository with full CRUD operations
- [x] Soft delete support
- [x] Database factory pattern
- [x] Configuration-based database selection
- [x] Dependency injection setup
- [x] Basic API configuration with Swagger
- [x] Async/await patterns throughout

### 🚧 In Progress

- [ ] SQL Server DbContext implementation
- [ ] Domain entities implementation
- [ ] Service layer development

### 📋 Planned

- [ ] API controllers
- [ ] Authentication and authorization
- [ ] Validation and error handling
- [ ] Real-time features
- [ ] Testing implementation
- [ ] AutoMapper integration
- [ ] Logging and monitoring

## 📚 API Documentation

### Current Endpoints

Currently, no API endpoints are implemented. The project is in the foundation phase with data access layer completed.

### Planned Endpoints

#### User Management

- `GET /api/users` - Get all users
- `GET /api/users/{id}` - Get user by ID
- `POST /api/users` - Create new user
- `PUT /api/users/{id}` - Update user
- `DELETE /api/users/{id}` - Delete user

#### Bus Management

- `GET /api/buses` - Get all buses
- `GET /api/buses/{id}` - Get bus by ID
- `POST /api/buses` - Add new bus
- `PUT /api/buses/{id}` - Update bus
- `DELETE /api/buses/{id}` - Delete bus

#### Route Management

- `GET /api/routes` - Get all routes
- `GET /api/routes/{id}` - Get route by ID
- `POST /api/routes` - Create new route
- `PUT /api/routes/{id}` - Update route
- `DELETE /api/routes/{id}` - Delete route

#### Booking Management

- `GET /api/bookings` - Get all bookings
- `GET /api/bookings/{id}` - Get booking by ID
- `POST /api/bookings` - Create new booking
- `PUT /api/bookings/{id}` - Update booking
- `DELETE /api/bookings/{id}` - Cancel booking

### Testing the API

Use the provided HTTP file for testing:

```bash
# Test the API using the .http file
APIs/APIs.http
```

## 📖 Documentation

### Architecture Documentation

- **[Project Architecture](PROJECT_ARCHITECTURE.md)**: Detailed architecture overview with dual database support
- **[Development Guide](DEVELOPMENT_GUIDE.md)**: Comprehensive guide for team development

### Key Concepts

#### Repository Pattern

The project implements separate repository patterns for each database type:

**SQL Server Repository**:

```csharp
public interface ISqlRepository<T> where T : BaseDomain
{
    Task<IEnumerable<T>> FindAllAsync();
    Task<T?> FindAsync(int id);
    Task<T> AddAsync(T entity);
    Task<T?> UpdateAsync(T entity);
    Task<T?> DeleteAsync(T entity);
    Task<IEnumerable<T>> FindByConditionAsync(Expression<Func<T, bool>> expression);
}
```

**MongoDB Repository**:

```csharp
public interface IMongoRepository<T> where T : BaseMongoDocument
{
    Task<IEnumerable<T>> FindAllAsync();
    Task<T?> FindAsync(string id);
    Task<T> AddAsync(T document);
    Task<T?> UpdateAsync(T document);
    Task<T?> DeleteAsync(string id);
    Task<IEnumerable<T>> FindByConditionAsync(Expression<Func<T, bool>> expression);
}
```

#### Base Models

**SQL Server Entities** (inherit from `BaseDomain`):

```csharp
public class BaseDomain
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
}
```

**MongoDB Documents** (inherit from `BaseMongoDocument`):

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

#### Database Factory

Configuration-based database selection:

```csharp
public interface IDatabaseFactory
{
    T GetRepository<T>() where T : class;
    DatabaseType GetDefaultDatabaseType();
    bool IsDatabaseEnabled(DatabaseType databaseType);
}
```

## 🤝 Contributing

### Development Guidelines

1. **Code Style**: Follow C# naming conventions
2. **Async Operations**: Use async/await for I/O operations
3. **Error Handling**: Implement proper exception handling
4. **Documentation**: Add XML comments for public APIs
5. **Testing**: Write unit tests for business logic
6. **Database Selection**: Use appropriate repository based on entity type
7. **Repository Usage**: Follow the established patterns for each database type

### Branch Strategy

- `main`: Production-ready code
- `develop`: Development branch
- `feature/*`: Feature branches
- `hotfix/*`: Hotfix branches

### Pull Request Process

1. Create a feature branch from `develop`
2. Implement your changes
3. Add tests if applicable
4. Update documentation
5. Create a pull request to `develop`

## 🐛 Issues and Support

### Reporting Issues

- Use GitHub Issues for bug reports
- Provide detailed reproduction steps
- Include environment information
- Specify database type if relevant

### Getting Help

- Check the documentation first
- Review existing issues
- Create a new issue with clear description

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- **.NET Team**: For the excellent framework
- **Entity Framework Team**: For the powerful ORM
- **MongoDB Team**: For the MongoDB.Driver
- **Clean Architecture**: For the architectural principles

---
