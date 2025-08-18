using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace APIs.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public HealthController(
        HealthCheckService healthCheckService,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _healthCheckService = healthCheckService;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _environment = environment;
    }

    /// <summary>
    /// Liveness probe - checks if the application is running
    /// </summary>
    /// <returns>200 OK if application is alive</returns>
    [HttpGet("live")]
    public IActionResult Live()
    {
        var process = Process.GetCurrentProcess();
        var uptime = DateTime.UtcNow - process.StartTime.ToUniversalTime();
        
        return Ok(new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            uptime = Environment.TickCount64,
            process = new
            {
                id = process.Id,
                name = process.ProcessName,
                startTime = process.StartTime.ToUniversalTime(),
                uptime = uptime.TotalSeconds,
                memoryUsage = new
                {
                    workingSet = process.WorkingSet64,
                    privateMemory = process.PrivateMemorySize64,
                    virtualMemory = process.VirtualMemorySize64
                },
                threads = process.Threads.Count,
                handles = process.HandleCount
            },
            environment = new
            {
                name = _environment.EnvironmentName,
                isDevelopment = _environment.IsDevelopment(),
                isProduction = _environment.IsProduction(),
                isStaging = _environment.IsStaging(),
                applicationName = _environment.ApplicationName,
                contentRootPath = _environment.ContentRootPath,
                webRootPath = _environment.WebRootPath
            },
            system = new
            {
                os = RuntimeInformation.OSDescription,
                architecture = RuntimeInformation.OSArchitecture,
                framework = RuntimeInformation.FrameworkDescription,
                runtime = RuntimeInformation.RuntimeIdentifier,
                processorCount = Environment.ProcessorCount,
                machineName = Environment.MachineName,
                userName = Environment.UserName
            }
        });
    }

    /// <summary>
    /// Readiness probe - checks if the application is ready to serve requests
    /// </summary>
    /// <returns>200 OK if all dependencies are healthy, 503 if any dependency is unhealthy</returns>
    [HttpGet("ready")]
    public async Task<IActionResult> Ready()
    {
        var healthReport = await _healthCheckService.CheckHealthAsync(registration =>
            registration.Tags.Contains("ready"));

        var databaseInfo = await GetDatabaseInformation();
        var connectionInfo = await GetConnectionInformation();

        var response = new
        {
            status = healthReport.Status.ToString(),
            timestamp = DateTime.UtcNow,
            checks = healthReport.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                duration = entry.Value.Duration.TotalMilliseconds,
                exception = entry.Value.Exception?.Message,
                data = entry.Value.Data
            }),
            databases = databaseInfo,
            connections = connectionInfo,
            environment = new
            {
                name = _environment.EnvironmentName,
                isDevelopment = _environment.IsDevelopment(),
                isProduction = _environment.IsProduction()
            }
        };

        return healthReport.Status == HealthStatus.Healthy 
            ? Ok(response) 
            : StatusCode(503, response);
    }

    /// <summary>
    /// Detailed health check with all registered health checks
    /// </summary>
    /// <returns>Detailed health report</returns>
    [HttpGet]
    public async Task<IActionResult> Health()
    {
        var healthReport = await _healthCheckService.CheckHealthAsync();
        var databaseInfo = await GetDatabaseInformation();
        var connectionInfo = await GetConnectionInformation();
        var systemInfo = await GetSystemInformation();

        var response = new
        {
            status = healthReport.Status.ToString(),
            timestamp = DateTime.UtcNow,
            totalDuration = healthReport.TotalDuration.TotalMilliseconds,
            checks = healthReport.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                duration = entry.Value.Duration.TotalMilliseconds,
                exception = entry.Value.Exception?.Message,
                data = entry.Value.Data,
                tags = entry.Value.Tags
            }),
            databases = databaseInfo,
            connections = connectionInfo,
            system = systemInfo,
            environment = new
            {
                name = _environment.EnvironmentName,
                isDevelopment = _environment.IsDevelopment(),
                isProduction = _environment.IsProduction(),
                isStaging = _environment.IsStaging(),
                applicationName = _environment.ApplicationName,
                contentRootPath = _environment.ContentRootPath,
                webRootPath = _environment.WebRootPath
            },
            configuration = new
            {
                connectionStrings = new
                {
                    sqlServer = GetConnectionStringInfo("SqlServer"),
                    mongoDb = GetConnectionStringInfo("MongoDb")
                },
                databaseSettings = _configuration.GetSection("DatabaseSettings").Get<object>()
            }
        };

        return healthReport.Status == HealthStatus.Healthy 
            ? Ok(response) 
            : StatusCode(503, response);
    }

    private async Task<object> GetDatabaseInformation()
    {
        dynamic info = new
        {
            sqlServer = new
            {
                status = "Unknown",
                connectionString = GetConnectionStringInfo("SqlServer"),
                details = new
                {
                    provider = "Entity Framework Core",
                    version = "9.0.8"
                }
            },
            mongoDb = new
            {
                status = "Unknown",
                connectionString = GetConnectionStringInfo("MongoDb"),
                details = new
                {
                    provider = "MongoDB.Driver",
                    version = "3.4.2"
                }
            }
        };

        // Test SQL Server connection
        try
        {
            var sqlContext = _serviceProvider.GetService<Data.Contexts.SqlServer.EduBusSqlContext>();
            if (sqlContext != null)
            {
                var canConnect = await sqlContext.Database.CanConnectAsync();
                info = new
                {
                    sqlServer = new
                    {
                        status = canConnect ? "Connected" : "Disconnected",
                        connectionString = GetConnectionStringInfo("SqlServer"),
                        details = new
                        {
                            provider = "Entity Framework Core",
                            version = "9.0.8",
                            databaseName = sqlContext.Database.GetDbConnection().Database,
                            server = sqlContext.Database.GetDbConnection().DataSource,
                            connectionState = sqlContext.Database.GetDbConnection().State.ToString()
                        }
                    },
                    mongoDb = info.mongoDb
                };
            }
        }
        catch (Exception ex)
        {
            info = new
            {
                sqlServer = new
                {
                    status = "Error",
                    connectionString = GetConnectionStringInfo("SqlServer"),
                    details = new
                    {
                        provider = "Entity Framework Core",
                        version = "9.0.8",
                        error = ex.Message
                    }
                },
                mongoDb = info.mongoDb
            };
        }

        // Test MongoDB connection
        try
        {
            var mongoClient = _serviceProvider.GetService<IMongoClient>();
            if (mongoClient != null)
            {
                var database = mongoClient.GetDatabase("admin");
                var result = await database.RunCommandAsync<dynamic>(new MongoDB.Bson.BsonDocument("ping", 1));
                info = new
                {
                    sqlServer = info.sqlServer,
                    mongoDb = new
                    {
                        status = "Connected",
                        connectionString = GetConnectionStringInfo("MongoDb"),
                        details = new
                        {
                            provider = "MongoDB.Driver",
                            version = "3.4.2",
                            server = mongoClient.Settings.Server.ToString(),
                            database = "edubus"
                        }
                    }
                };
            }
        }
        catch (Exception ex)
        {
            info = new
            {
                sqlServer = info.sqlServer,
                mongoDb = new
                {
                    status = "Error",
                    connectionString = GetConnectionStringInfo("MongoDb"),
                    details = new
                    {
                        provider = "MongoDB.Driver",
                        version = "3.4.2",
                        error = ex.Message
                    }
                }
            };
        }

        return info;
    }

    private async Task<object> GetConnectionInformation()
    {
        var process = Process.GetCurrentProcess();
        
        return new
        {
            activeConnections = new
            {
                tcp = GetTcpConnections(),
                udp = GetUdpConnections()
            },
            process = new
            {
                id = process.Id,
                handles = process.HandleCount,
                threads = process.Threads.Count,
                memory = new
                {
                    workingSet = process.WorkingSet64,
                    privateMemory = process.PrivateMemorySize64,
                    virtualMemory = process.VirtualMemorySize64,
                    peakWorkingSet = process.PeakWorkingSet64,
                    peakVirtualMemory = process.PeakVirtualMemorySize64
                }
            }
        };
    }

    private async Task<object> GetSystemInformation()
    {
        var process = Process.GetCurrentProcess();
        var uptime = DateTime.UtcNow - process.StartTime.ToUniversalTime();
        
        return new
        {
            os = new
            {
                description = RuntimeInformation.OSDescription,
                architecture = RuntimeInformation.OSArchitecture,
                platform = Environment.OSVersion.Platform.ToString()
            },
            runtime = new
            {
                framework = RuntimeInformation.FrameworkDescription,
                identifier = RuntimeInformation.RuntimeIdentifier,
                version = Environment.Version.ToString()
            },
            hardware = new
            {
                processorCount = Environment.ProcessorCount,
                machineName = Environment.MachineName,
                userName = Environment.UserName,
                systemPageSize = Environment.SystemPageSize,
                workingSet = Environment.WorkingSet
            },
            process = new
            {
                id = process.Id,
                name = process.ProcessName,
                startTime = process.StartTime.ToUniversalTime(),
                uptime = uptime.TotalSeconds,
                totalProcessorTime = process.TotalProcessorTime.TotalSeconds,
                userProcessorTime = process.UserProcessorTime.TotalSeconds
            }
        };
    }

    private object GetConnectionStringInfo(string key)
    {
        var connectionString = _configuration.GetConnectionString(key);
        if (string.IsNullOrEmpty(connectionString))
        {
            return new { status = "Not Configured", value = "" };
        }

        // Mask sensitive information
        var maskedConnectionString = MaskConnectionString(connectionString);
        
        return new
        {
            status = "Configured",
            value = maskedConnectionString,
            length = connectionString.Length
        };
    }

    private string MaskConnectionString(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return connectionString;

        // Mask password in connection string
        var masked = connectionString;
        
        // SQL Server password masking
        masked = System.Text.RegularExpressions.Regex.Replace(
            masked, 
            @"Password=([^;]+)", 
            "Password=***"
        );
        
        // MongoDB password masking
        masked = System.Text.RegularExpressions.Regex.Replace(
            masked, 
            @"mongodb://([^:]+):([^@]+)@", 
            "mongodb://$1:***@"
        );
        
        return masked;
    }

    private object GetTcpConnections()
    {
        try
        {
            var connections = System.Net.NetworkInformation.IPGlobalProperties
                .GetIPGlobalProperties()
                .GetActiveTcpConnections()
                .Where(c => c.State == System.Net.NetworkInformation.TcpState.Established)
                .Count();
            
            return new { count = connections, state = "Established" };
        }
        catch
        {
            return new { count = 0, state = "Unknown" };
        }
    }

    private object GetUdpConnections()
    {
        try
        {
            var connections = System.Net.NetworkInformation.IPGlobalProperties
                .GetIPGlobalProperties()
                .GetActiveUdpListeners()
                .Count();
            
            return new { count = connections, state = "Listening" };
        }
        catch
        {
            return new { count = 0, state = "Unknown" };
        }
    }
}
