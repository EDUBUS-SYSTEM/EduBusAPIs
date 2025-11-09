using System.Linq.Expressions;
using Data.Models;
using MongoDB.Driver;

namespace Data.Repos.Interfaces
{
    public interface IMongoRepository<T> where T : BaseMongoDocument
    {
        Task<IEnumerable<T>> FindAllAsync();
        Task<T?> FindAsync(Guid id);
        Task<T> AddAsync(T document);
        Task<T?> UpdateAsync(T document);
        Task<T?> DeleteAsync(Guid id);
        Task<IEnumerable<T>> FindByConditionAsync(Expression<Func<T, bool>> expression);
        Task<IEnumerable<T>> FindByFilterAsync(FilterDefinition<T> filter);
        Task<IEnumerable<T>> FindByFilterAsync(FilterDefinition<T> filter, SortDefinition<T> sort);
        Task<IEnumerable<T>> FindByFilterAsync(FilterDefinition<T> filter, int skip, int limit);
        Task<IEnumerable<T>> FindByFilterAsync(FilterDefinition<T> filter, SortDefinition<T> sort, int skip, int limit);
        Task<long> GetCountAsync();
		Task<long> GetCountAsync(FilterDefinition<T> filter);
		Task<bool> ExistsAsync(Guid id);
        Task<T?> FindOneAndUpdateAsync(FilterDefinition<T> filter, UpdateDefinition<T> update);
        Task<T?> FindOneAndDeleteAsync(FilterDefinition<T> filter);
		Task<IEnumerable<T>> BulkCreateAsync(IEnumerable<T> documents);
		Task<BulkWriteResult> BulkDeleteAsync(IEnumerable<Guid> ids);
		Task<IEnumerable<T>> BulkUpdateAsync(IEnumerable<T> documents);
	}
}