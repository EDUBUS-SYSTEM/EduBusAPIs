using System.Linq.Expressions;
using Data.Models;
using MongoDB.Driver;

namespace Data.Repos.Interfaces
{
    public interface IMongoRepository<T> where T : BaseMongoDocument
    {
        Task<IEnumerable<T>> FindAllAsync();
        Task<T?> FindAsync(string id);
        Task<T> AddAsync(T document);
        Task<T?> UpdateAsync(T document);
        Task<T?> DeleteAsync(string id);
        Task<IEnumerable<T>> FindByConditionAsync(Expression<Func<T, bool>> expression);
        Task<IEnumerable<T>> FindByFilterAsync(FilterDefinition<T> filter);
        Task<IEnumerable<T>> FindByFilterAsync(FilterDefinition<T> filter, SortDefinition<T> sort);
        Task<IEnumerable<T>> FindByFilterAsync(FilterDefinition<T> filter, int skip, int limit);
        Task<long> GetCountAsync();
        Task<bool> ExistsAsync(string id);
        Task<T?> FindOneAndUpdateAsync(FilterDefinition<T> filter, UpdateDefinition<T> update);
        Task<T?> FindOneAndDeleteAsync(FilterDefinition<T> filter);
    }
}
