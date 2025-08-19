using System.Linq.Expressions;
using Data.Models;

namespace Data.Repos.Interfaces
{
    public interface ISqlRepository<T> where T : BaseDomain
    {
        Task<IEnumerable<T>> FindAllAsync();
        Task<IEnumerable<T>> FindAllAsync(params Expression<Func<T, object>>[] includes);
        Task<T?> FindAsync(Guid id);
        Task<T> AddAsync(T entity);
        Task<T?> UpdateAsync(T entity);
        Task<T?> DeleteAsync(T entity);
        Task<IEnumerable<T>> FindByConditionAsync(Expression<Func<T, bool>> expression);
        Task<IEnumerable<T>> FindByConditionAsync(Expression<Func<T, bool>> expression, params Expression<Func<T, object>>[] includes);
        Task<int> GetCountAsync();
        Task<bool> ExistsAsync(Guid id);
    }
}
