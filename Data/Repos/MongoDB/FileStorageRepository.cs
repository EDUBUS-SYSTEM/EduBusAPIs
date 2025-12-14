using Data.Models;
using Data.Repos.Interfaces;
using MongoDB.Driver;

namespace Data.Repos.MongoDB
{
    public class FileStorageRepository : MongoRepository<FileStorage>, IFileStorageRepository
    {
        public FileStorageRepository(IMongoDatabase database) : base(database, "file_storage")
        {
        }

        public async Task<FileStorage?> GetActiveFileByEntityAsync(Guid entityId, string entityType, string fileType)
        {
            var filter = Builders<FileStorage>.Filter.And(
                Builders<FileStorage>.Filter.Eq(x => x.EntityId, entityId),
                Builders<FileStorage>.Filter.Eq(x => x.EntityType, entityType),
                Builders<FileStorage>.Filter.Eq(x => x.FileType, fileType),
                Builders<FileStorage>.Filter.Eq(x => x.IsActive, true)
            );

            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<FileStorage>> GetFilesByEntityAsync(Guid entityId, string entityType)
        {
            var filter = Builders<FileStorage>.Filter.And(
                Builders<FileStorage>.Filter.Eq(x => x.EntityId, entityId),
                Builders<FileStorage>.Filter.Eq(x => x.EntityType, entityType)
            );

            return await _collection.Find(filter).ToListAsync();
        }

        public async Task<bool> DeactivateFileAsync(Guid fileId)
        {
            var filter = Builders<FileStorage>.Filter.Eq(x => x.Id, fileId);
            var update = Builders<FileStorage>.Update
                .Set(x => x.IsActive, false)
                .Set(x => x.IsDeleted, true)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);

            var result = await _collection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }
    }
}
