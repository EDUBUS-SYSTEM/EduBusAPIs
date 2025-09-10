using Data.Contexts.MongoDB;
using Data.Contexts.SqlServer;
using Data.Repos.Interfaces;
using Data.Repos.MongoDB;
using Data.Repos.SqlServer;
using Microsoft.EntityFrameworkCore;
using Utils;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Services.Contracts;
using Services.Implementations;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Services.MapperProfiles;
using APIs.Hubs;
using Data.Models;
using Services.Models.Configuration;
using StackExchange.Redis;
using System.Security.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Configure MongoDB Guid representation
BsonSerializer.RegisterSerializer(typeof(Guid), new GuidSerializer(GuidRepresentation.Standard));

// Load configuration 
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddUserSecrets<Program>(optional: true)   
    .AddEnvironmentVariables();

// Configure settings with reload on change
builder.Services.Configure<LeaveRequestSettings>(
    builder.Configuration.GetSection("LeaveRequestSettings"));                

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add SignalR with CORS support
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
    
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",     // React development
                "http://localhost:3001",     // React alternative port
                "https://localhost:3000",    // React HTTPS
                "https://localhost:3001",    // React HTTPS alternative
                "http://localhost:5223",     // API HTTP
                "https://localhost:7061"     // API HTTPS
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "EduBus APIs", Version = "v1" });
    var jwtScheme = new OpenApiSecurityScheme
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Description = "Enter 'Bearer {your token}'.",
        Reference = new OpenApiReference { Id = "Bearer", Type = ReferenceType.SecurityScheme }
    };
    c.AddSecurityDefinition("Bearer", jwtScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { jwtScheme, Array.Empty<string>() } });
});

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
    .AddDbContextCheck<EduBusSqlContext>("sql_server", tags: new[] { "ready" })
    .AddMongoDb(
        sp => new MongoClient(builder.Configuration.GetConnectionString("MongoDb") ?? "mongodb://localhost:27017/edubus"),
        name: "mongodb",
        tags: new[] { "ready" },
        timeout: TimeSpan.FromSeconds(3)
    );

// DatabaseFactory
builder.Services.AddScoped<IDatabaseFactory, DatabaseFactory>();

// --- SQL Server Configuration ---
var sqlConnectionString = builder.Configuration.GetConnectionString("SqlServer");
builder.Services.AddDbContext<EduBusSqlContext>(options =>
    options.UseSqlServer(
        sqlConnectionString,
        sqlOptions =>
        {
            sqlOptions.UseNetTopologySuite();
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null
            );
        }
    )
);

// --- MongoDB Configuration ---
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDb");
var mongoUrl = new MongoUrl(mongoConnectionString);
builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoUrl));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IMongoClient>().GetDatabase(mongoUrl.DatabaseName));
builder.Services.AddSingleton<EduBusMongoContext>();

// --- Redis/OTP Store Configuration ---
// For development, use in-memory OTP store to avoid external dependencies.
builder.Services.AddSingleton<IOtpStore, InMemoryOtpStore>();

// If you want to switch back to Redis, replace the above with:
// var redisConnectionString = builder.Configuration["Redis:ConnectionString"];
// builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
// builder.Services.AddSingleton<IOtpStore, RedisOtpStore>();

// Repository Registration
builder.Services.AddScoped(typeof(ISqlRepository<>), typeof(SqlRepository<>));

// Repository Registration for SqlServer
builder.Services.AddScoped<IUserAccountRepository, UserAccountRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IParentRepository, ParentRepository>();
builder.Services.AddScoped<IDriverRepository, DriverRepository>();
builder.Services.AddScoped<IDriverLicenseRepository, DriverLicenseRepository>();
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IDriverVehicleRepository, DriverVehicleRepository>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IGradeRepository, GradeRepository>();
builder.Services.AddScoped<IStudentGradeRepository, StudentGradeRepository>();
builder.Services.AddScoped<IDriverLeaveRepository, DriverLeaveRepository>();
builder.Services.AddScoped<IDriverLeaveConflictRepository, DriverLeaveConflictRepository>();
builder.Services.AddScoped<IDriverWorkingHoursRepository, DriverWorkingHoursRepository>();
builder.Services.AddScoped<IPickupPointRepository, PickupPointRepository>();
builder.Services.AddScoped<IStudentPickupPointHistoryRepository, StudentPickupPointHistoryRepository>();

// Repository Registration for MongoDB
builder.Services.AddScoped<IFileStorageRepository, FileStorageRepository>();
builder.Services.AddScoped<IMongoRepository<Notification>, NotificationRepository>();
builder.Services.AddScoped<IMongoRepository<Data.Models.Route>, RouteRepository>();
builder.Services.AddScoped<IPickupPointRequestRepository, PickupPointRequestRepository>();
builder.Services.AddScoped<IParentRegistrationRepository, ParentRegistrationRepository>();

// Services Registration
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IParentService, ParentService>();
builder.Services.AddScoped<IDriverService, DriverService>();
builder.Services.AddScoped<IDriverLicenseService, DriverLicenseService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IDriverVehicleService, DriverVehicleService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IStudentGradeService, StudentGradeService>();
builder.Services.AddScoped<IDriverLeaveService, DriverLeaveService>();
builder.Services.AddScoped<IDriverWorkingHoursService, DriverWorkingHoursService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
builder.Services.AddScoped<IPickupPointEnrollmentService, PickupPointEnrollmentService>();
builder.Services.AddScoped<IOtpStore, InMemoryOtpStore>();

// SignalR Hub Service
builder.Services.AddScoped<Services.Contracts.INotificationHubService, APIs.Services.NotificationHubService>();

// Background Services
builder.Services.AddHostedService<Services.Backgrounds.RefreshTokenCleanupService>();
builder.Services.AddHostedService<Services.Backgrounds.AutoReplacementSuggestionService>();
builder.Services.AddHostedService<Services.Backgrounds.NotificationCleanupService>();

// Register DbContext for SqlRepository
builder.Services.AddScoped<DbContext>(provider => provider.GetRequiredService<EduBusSqlContext>());
// Register Parent AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// JWT Authentication 
var jwt = builder.Configuration.GetSection("Jwt");
var jwtKey = jwt["Key"];

if (string.IsNullOrEmpty(jwtKey))
{
    throw new InvalidOperationException("JWT Key is not configured. Please set Jwt:Key in user-secrets (Dev) or environment variables (Prod).");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.RequireHttpsMetadata = true;
        o.SaveToken = true;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwt["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Enable static files
app.UseStaticFiles();

// Use CORS - Choose one policy based on your needs
if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowAll"); // Allow all origins in development
}
else
{
    app.UseCors("AllowSpecificOrigins"); // Restrict to specific origins in production
}

app.UseAuthentication();
app.UseAuthorization();

// Map SignalR Hub with CORS support
app.MapHub<NotificationHub>("/notificationHub", options =>
{
    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets | 
                        Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
});

app.MapControllers();

// Map Health Check endpoints
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            uptime = Environment.TickCount64
        };
        await context.Response.WriteAsJsonAsync(response);
    }
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                duration = entry.Value.Duration.TotalMilliseconds
            })
        };
        await context.Response.WriteAsJsonAsync(response);
    }
});

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            totalDuration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                duration = entry.Value.Duration.TotalMilliseconds,
                tags = entry.Value.Tags
            })
        };
        await context.Response.WriteAsJsonAsync(response);
    }
});

app.Run();
