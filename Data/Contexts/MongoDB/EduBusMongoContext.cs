using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using MongoDB.Bson;

namespace Data.Contexts.MongoDB
{
    public class EduBusMongoContext
    {
        private readonly IMongoDatabase _database;
        private readonly IConfiguration _configuration;

        public EduBusMongoContext(IConfiguration configuration)
        {
            _configuration = configuration;
            var connectionString = configuration.GetConnectionString("MongoDB");
            var databaseName = configuration["MongoDB:DatabaseName"] ?? "EduBusDB";
            
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException("MongoDB connection string is not configured");
            }

            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }

        public IMongoDatabase Database => _database;

        public IMongoCollection<T> GetCollection<T>(string collectionName)
        {
            return _database.GetCollection<T>(collectionName);
        }

        public IMongoCollection<T> GetCollection<T>()
        {
            // Use the type name as collection name
            var collectionName = typeof(T).Name.ToLowerInvariant();
            return _database.GetCollection<T>(collectionName);
        }

        public async Task<bool> IsConnectedAsync()
        {
            try
            {
                await _database.RunCommandAsync((Command<BsonDocument>)"{ping:1}");
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task CreateIndexesAsync()
        {
            // Create indexes for collections here
            // Example:
            // var userCollection = GetCollection<User>("users");
            // var indexKeysDefinition = Builders<User>.IndexKeys.Ascending(x => x.Email);
            // var indexModel = new CreateIndexModel<User>(indexKeysDefinition, new CreateIndexOptions { Unique = true });
            // await userCollection.Indexes.CreateOneAsync(indexModel);
        }
    }
}
