using Data.Models;
using Data.Repos.Interfaces;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace Data.Repos.MongoDB
{
    public class MongoRepository<T> : IMongoRepository<T> where T : BaseMongoDocument
    {
        protected readonly IMongoCollection<T> _collection;

        public MongoRepository(IMongoDatabase database, string collectionName)
        {
            _collection = database.GetCollection<T>(collectionName);
        }

        public virtual async Task<IEnumerable<T>> FindAllAsync()
        {
            var filter = Builders<T>.Filter.Eq(x => x.IsDeleted, false);
            return await _collection.Find(filter).ToListAsync();
        }

        public virtual async Task<T?> FindAsync(Guid id)
        {
            var filter = Builders<T>.Filter.And(
                Builders<T>.Filter.Eq(x => x.Id, id),
                Builders<T>.Filter.Eq(x => x.IsDeleted, false)
            );
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public virtual async Task<T> AddAsync(T document)
        {
            document.CreatedAt = DateTime.UtcNow;
            await _collection.InsertOneAsync(document);
            return document;
        }

        public virtual async Task<T?> UpdateAsync(T document)
        {
            if (document != null)
            {
                document.UpdatedAt = DateTime.UtcNow;
                var filter = Builders<T>.Filter.Eq(x => x.Id, document.Id);
                var update = Builders<T>.Update
                    .Set(x => x.UpdatedAt, document.UpdatedAt);

                // Use reflection to update all properties except Id, CreatedAt, and IsDeleted
                var properties = typeof(T).GetProperties()
                    .Where(p => p.Name != nameof(BaseMongoDocument.Id) && 
                               p.Name != nameof(BaseMongoDocument.CreatedAt) && 
                               p.Name != nameof(BaseMongoDocument.IsDeleted));

                foreach (var property in properties)
                {
                    var value = property.GetValue(document);
                    update = update.Set(property.Name, value);
                }

                var result = await _collection.UpdateOneAsync(filter, update);
                if (result.ModifiedCount > 0)
                {
                    return await FindAsync(document.Id);
                }
            }
            return null;
        }

        public virtual async Task<T?> DeleteAsync(Guid id)
        {
            var filter = Builders<T>.Filter.Eq(x => x.Id, id);
            var update = Builders<T>.Update
                .Set(x => x.IsDeleted, true)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);

            var result = await _collection.UpdateOneAsync(filter, update);
            if (result.ModifiedCount > 0)
            {
                return await FindAsync(id);
            }
            return null;
        }

        public virtual async Task<IEnumerable<T>> FindByConditionAsync(Expression<Func<T, bool>> expression)
        {
            var filter = Builders<T>.Filter.And(
                expression,
                Builders<T>.Filter.Eq(x => x.IsDeleted, false)
            );
            return await _collection.Find(filter).ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> FindByFilterAsync(FilterDefinition<T> filter)
        {
            var combinedFilter = Builders<T>.Filter.And(
                filter,
                Builders<T>.Filter.Eq(x => x.IsDeleted, false)
            );
            return await _collection.Find(combinedFilter).ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> FindByFilterAsync(FilterDefinition<T> filter, SortDefinition<T> sort)
        {
            var combinedFilter = Builders<T>.Filter.And(
                filter,
                Builders<T>.Filter.Eq(x => x.IsDeleted, false)
            );
            return await _collection.Find(combinedFilter).Sort(sort).ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> FindByFilterAsync(FilterDefinition<T> filter, int skip, int limit)
        {
            var combinedFilter = Builders<T>.Filter.And(
                filter,
                Builders<T>.Filter.Eq(x => x.IsDeleted, false)
            );
            return await _collection.Find(combinedFilter).Skip(skip).Limit(limit).ToListAsync();
        }

        public virtual async Task<long> GetCountAsync()
        {
            var filter = Builders<T>.Filter.Eq(x => x.IsDeleted, false);
            return await _collection.CountDocumentsAsync(filter);
        }

        public virtual async Task<bool> ExistsAsync(Guid id)
        {
            var filter = Builders<T>.Filter.And(
                Builders<T>.Filter.Eq(x => x.Id, id),
                Builders<T>.Filter.Eq(x => x.IsDeleted, false)
            );
            return await _collection.Find(filter).AnyAsync();
        }

        public virtual async Task<T?> FindOneAndUpdateAsync(FilterDefinition<T> filter, UpdateDefinition<T> update)
        {
            var combinedFilter = Builders<T>.Filter.And(
                filter,
                Builders<T>.Filter.Eq(x => x.IsDeleted, false)
            );
            var options = new FindOneAndUpdateOptions<T>
            {
                ReturnDocument = ReturnDocument.After
            };
            return await _collection.FindOneAndUpdateAsync(combinedFilter, update, options);
        }

        public virtual async Task<T?> FindOneAndDeleteAsync(FilterDefinition<T> filter)
        {
            var combinedFilter = Builders<T>.Filter.And(
                filter,
                Builders<T>.Filter.Eq(x => x.IsDeleted, false)
            );
            return await _collection.FindOneAndDeleteAsync(combinedFilter);
        }
    }
}
