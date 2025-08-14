using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Services
{
    public class TestService
    {
        private readonly IMongoClient _mongoClient;

        public TestService(IMongoClient mongoClient)
        {
            _mongoClient = mongoClient;
        }

        public async Task TestInsert()
        {
            var database = _mongoClient.GetDatabase("EduBus");
            var collection = database.GetCollection<BsonDocument>("test");

            var doc = new BsonDocument
        {
            { "message", "Hello from DI" },
            { "createdAt", DateTime.UtcNow }
        };

            await collection.InsertOneAsync(doc);

            var result = await collection.Find(new BsonDocument()).FirstOrDefaultAsync();
            Console.WriteLine(result.ToJson());
        }
    }

}
