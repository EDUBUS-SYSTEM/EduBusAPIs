using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Utils
{
    public enum DatabaseType
    {
        SqlServer,
        MongoDb
    }

    public interface IDatabaseFactory
    {
        T GetRepository<T>() where T : class;
        DatabaseType GetDefaultDatabaseType();
        bool IsDatabaseEnabled(DatabaseType databaseType);
    }

    public class DatabaseFactory : IDatabaseFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public DatabaseFactory(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        public T GetRepository<T>() where T : class
        {
            var databaseType = GetDefaultDatabaseType();
            return GetRepositoryByType<T>(databaseType);
        }

        public T GetRepositoryByType<T>(DatabaseType databaseType) where T : class
        {
            return databaseType switch
            {
                DatabaseType.SqlServer => _serviceProvider.GetService<T>() ?? 
                    throw new InvalidOperationException($"SQL Server repository of type {typeof(T).Name} not registered"),
                DatabaseType.MongoDb => _serviceProvider.GetService<T>() ?? 
                    throw new InvalidOperationException($"MongoDb repository of type {typeof(T).Name} not registered"),
                _ => throw new ArgumentException($"Unsupported database type: {databaseType}")
            };
        }

        public DatabaseType GetDefaultDatabaseType()
        {
            var defaultDb = _configuration["DatabaseSettings:DefaultDatabase"];
            return defaultDb?.ToLowerInvariant() switch
            {
                "mongodb" => DatabaseType.MongoDb,
                "sqlserver" or "sql" => DatabaseType.SqlServer,
                _ => DatabaseType.SqlServer // Default fallback
            };
        }

        public bool IsDatabaseEnabled(DatabaseType databaseType)
        {
            var useMultipleDatabases = bool.TryParse(_configuration["DatabaseSettings:UseMultipleDatabases"], out var result) ? result : false;
            
            if (!useMultipleDatabases)
            {
                return databaseType == GetDefaultDatabaseType();
            }

            return databaseType switch
            {
                DatabaseType.SqlServer => !string.IsNullOrEmpty(_configuration.GetConnectionString("SqlServer")),
                DatabaseType.MongoDb => !string.IsNullOrEmpty(_configuration.GetConnectionString("MongoDb")),
                _ => false
            };
        }
    }
}
