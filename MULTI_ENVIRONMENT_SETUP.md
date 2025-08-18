# Multi-Environment Configuration Setup

## Overview

This guide covers the complete setup for multi-environment configuration in the EduBus APIs project, ensuring secure secret management across development and production environments.

## Configuration Structure

```
APIs/
├── appsettings.json              # Production config (empty connection strings)
├── appsettings.Development.json  # Development config (local databases)
└── Properties/
    └── launchSettings.json       # Environment variables (Development)
```

## Configuration Override Priority

1. `appsettings.json` (base)
2. `appsettings.{Environment}.json` (environment-specific)
3. **User Secrets** (Development only)
4. **Azure Connection Strings** (Production)
5. Command-line arguments

## Connection String Keys

- **SQL Server**: `ConnectionStrings:SqlServer`
- **MongoDb**: `ConnectionStrings:MongoDb`

## Environment Variables (Azure)

- `ConnectionStrings__SqlServer`
- `ConnectionStrings__MongoDb`

---

## Development Environment Setup

### Step 1: Initialize User Secrets

```bash
cd APIs
dotnet user-secrets init
```

### Step 2: Configure Connection Strings

```bash
# SQL Server LocalDB
dotnet user-secrets set "ConnectionStrings:SqlServer" "Server=(localdb)\MSSQLLocalDB;Database=edubus_dev;Trusted_Connection=True;Encrypt=False"

# MongoDb Local
dotnet user-secrets set "ConnectionStrings:MongoDb" "mongodb://localhost:27017/edubus"
```

### Step 3: Verify Configuration

```bash
dotnet user-secrets list
```

### Development Notes

- User Secrets only work in Development environment
- Connection strings override values in appsettings.Development.json
- Priority order: User Secrets > appsettings.Development.json > appsettings.json

---

## Production Environment Setup (Azure App Service)

### Step 1: Configure Connection Strings in Azure App Service (done)

#### SQL Server Connection String

1. Go to Azure Portal → App Service → Configuration → Connection strings (done)
2. Add new connection string (done)
   - **Name**: `SqlServer`
   - **Type**: `SQLAzure`
   - **Value**: `Server=tcp:your-server.database.windows.net,1433;Initial Catalog=YourDatabase;Persist Security Info=False;User ID=your-username;Password=your-password;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30`

#### MongoDb Connection String

1. Add second connection string:
   - **Name**: `MongoDb`
   - **Type**: `Custom`
   - **Value**: `mongodb+srv://username:password@cluster.mongodb.net/edubus?retryWrites=true&w=majority`

### Step 2: Configure Environment Variables (if needed)

1. Go to Configuration → Application settings
2. Add environment variables:
   - `ASPNETCORE_ENVIRONMENT`: `Production`

### Step 3: Save and Restart

1. Click "Save" to save configuration
2. Restart App Service to apply changes

### Production Notes

- Connection strings in Azure override all local configuration
- Ensure database name in MongoDb connection string is `edubus`
- Use Key Vault for sensitive secrets in production

---

## Security Best Practices ✅

- ✅ No secrets committed to repository
- ✅ Development: User Secrets
- ✅ Production: Azure Connection Strings/Key Vault
- ✅ Empty connection strings in appsettings.json
- ✅ Environment-specific configuration files
- ✅ Proper .gitignore configuration

## Health Checks ✅

The application includes comprehensive health checks:

- **Liveness Probe**: `/health/live` - Checks if application is running
- **Readiness Probe**: `/health/ready` - Checks if dependencies (SQL Server, MongoDb) are healthy
- **Detailed Health**: `/health` - Comprehensive health report

### Health Check Endpoints

```bash
# Liveness check (always returns 200 if app is running)
curl http://localhost:5223/health/live

# Readiness check (returns 200 if dependencies are healthy, 503 if unhealthy)
curl http://localhost:5223/health/ready

# Detailed health report
curl http://localhost:5223/health
```

### Kubernetes Integration

```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 80
  initialDelaySeconds: 30
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /health/ready
    port: 80
  initialDelaySeconds: 5
  periodSeconds: 5
```

## Code Implementation

The application uses the following pattern to read connection strings:

```csharp
// SQL Server Configuration
var sqlConnectionString = builder.Configuration.GetConnectionString("SqlServer");
builder.Services.AddDbContext<EduBusSqlContext>(options =>
    options.UseSqlServer(sqlConnectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null
        );
    })
);

// MongoDb Configuration
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDb");
var mongoUrl = new MongoUrl(mongoConnectionString);
builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoUrl));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IMongoClient>().GetDatabase(mongoUrl.DatabaseName));
```

## Quick Reference

| Environment | Configuration Method | Connection String Source |
| ----------- | -------------------- | ------------------------ |
| Development | User Secrets         | Local databases          |
| Production  | Azure App Service    | Cloud databases          |
| Staging     | Azure App Service    | Cloud databases          |

## Troubleshooting

### Common Issues

1. **User Secrets not working**: Ensure you're in Development environment
2. **Connection string not found**: Check the exact key names (`SqlServer`, `MongoDb`)
3. **Azure configuration not applied**: Restart the App Service after saving
4. **MongoDb connection failed**: Verify database name in connection string

### Verification Commands

```bash
# Check current environment
echo $env:ASPNETCORE_ENVIRONMENT

# List user secrets
dotnet user-secrets list

# Test connection strings
dotnet run --environment Development
```
