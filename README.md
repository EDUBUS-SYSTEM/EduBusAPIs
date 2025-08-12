# EduBus APIs

A comprehensive backend API system for educational bus management, built with .NET 8.0 and following clean architecture principles.

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

The system is built using modern .NET 8.0 technologies and follows clean architecture principles for maintainability and scalability.

## 🏗️ Architecture

The project follows **Clean Architecture** with clear separation of concerns:

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
├─────────────────────────────────────────────────────────────┤
│                  Infrastructure Layer                       │
│              (Utils & Constants Projects)                  │
└─────────────────────────────────────────────────────────────┘
```

### Architecture Principles

- **Separation of Concerns**: Each layer has a specific responsibility
- **Dependency Inversion**: High-level modules don't depend on low-level modules
- **Repository Pattern**: Abstraction over data access
- **Generic Repository**: Reusable data access for all entities
- **Async/Await**: Non-blocking operations throughout

## 🛠️ Technology Stack

### Core Technologies

- **.NET 8.0**: Latest LTS version
- **ASP.NET Core**: Web framework
- **Entity Framework Core**: ORM for data access
- **SQL Server**: Primary database

### Key Libraries

- **Swashbuckle.AspNetCore**: API documentation
- **AutoMapper**: Object mapping (planned)
- **FluentValidation**: Input validation (planned)
- **JWT Bearer**: Authentication (planned)

### Development Tools

- **Visual Studio 2022**: Primary IDE
- **Git**: Version control
- **Swagger UI**: API testing and documentation

## 📁 Project Structure

```
EduBusAPIs/
├── 📄 EduBusAPIs.sln              # Solution file
├── 🌐 APIs/                       # Web API Layer
│   ├── Controllers/               # API Controllers
│   ├── Program.cs                 # Application entry point
│   ├── appsettings.json          # Configuration
│   └── APIs.csproj               # Project file
├── ⚙️ Services/                   # Business Logic Layer
│   ├── Contracts/                # Service interfaces
│   ├── Implementations/          # Service implementations
│   ├── Models/                   # Business models/DTOs
│   └── MapperProfiles/           # AutoMapper profiles
├── 🗄️ Data/                      # Data Access Layer
│   ├── Models/                   # Domain entities
│   │   └── BaseDomain.cs         # Base entity class
│   └── Repos/                    # Repository implementations
│       ├── IRepository.cs        # Generic repository interface
│       └── Repository.cs         # Generic repository implementation
├── 🔧 Utils/                     # Utility Layer
└── 📋 Constants/                 # Constants Layer
```

## 🚀 Getting Started

### Prerequisites

- **.NET 8.0 SDK**: [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Visual Studio 2022** or **VS Code**
- **SQL Server** (LocalDB or full instance)

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

4. **Run the application**

   ```bash
   cd APIs
   dotnet run
   ```

5. **Access the API**
   - **API Base URL**: `https://localhost:7223` or `http://localhost:5223`
   - **Swagger UI**: `https://localhost:7223/swagger`

### Configuration

The application uses `appsettings.json` for configuration:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EduBusDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

## 📊 Development Status

### ✅ Completed

- [x] Solution structure and project setup
- [x] Clean architecture implementation
- [x] Repository pattern with generic implementation
- [x] Base domain model
- [x] Basic API configuration with Swagger
- [x] Async/await patterns throughout

### 🚧 In Progress

- [ ] Database context configuration
- [ ] Domain entities implementation
- [ ] Service layer development

### 📋 Planned

- [ ] API controllers
- [ ] Authentication and authorization
- [ ] Validation and error handling
- [ ] Real-time features
- [ ] Testing implementation

## 📚 API Documentation

### Current Endpoints

Currently, no API endpoints are implemented. The project is in the foundation phase.

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

- **[Project Architecture](PROJECT_ARCHITECTURE.md)**: Detailed architecture overview
- **[Code Structure Analysis](CODE_STRUCTURE.md)**: In-depth code analysis
- **[Development Roadmap](DEVELOPMENT_ROADMAP.md)**: Implementation plan and timeline

### Key Concepts

#### Repository Pattern

The project implements a generic repository pattern for data access:

```csharp
public interface IRepository<T>
{
    Task<IEnumerable<T>> FindAllAsync();
    Task<T?> FindAsync(int id);
    Task<T> AddAsync(T entity);
    Task<T?> UpdateAsync(T entity);
    Task<T?> DeleteAsync(T entity);
    Task<IEnumerable<T>> FindByConditionAsync(Expression<Func<T, bool>> expression);
}
```

#### Base Domain Model

All entities inherit from `BaseDomain`:

```csharp
public class BaseDomain
{
    public int Id { get; set; }
}
```

## 🤝 Contributing

### Development Guidelines

1. **Code Style**: Follow C# naming conventions
2. **Async Operations**: Use async/await for I/O operations
3. **Error Handling**: Implement proper exception handling
4. **Documentation**: Add XML comments for public APIs
5. **Testing**: Write unit tests for business logic

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

### Getting Help

- Check the documentation first
- Review existing issues
- Create a new issue with clear description

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- **.NET Team**: For the excellent framework
- **Entity Framework Team**: For the powerful ORM
- **Clean Architecture**: For the architectural principles

---

**Note**: This project is currently in active development. The API endpoints and features mentioned are planned and will be implemented according to the development roadmap.
