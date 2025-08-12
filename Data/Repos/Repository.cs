using Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Linq.Expressions;

namespace Data.Repos
{
    public class Repository<T> : IRepository<T> where T : BaseDomain
    {
        protected readonly DbContext _dbContext;
        protected readonly DbSet<T> _table;
        public DatabaseFacade Database => _dbContext.Database;
        public Repository(DbContext dbContext)
        {
            _dbContext = dbContext;
            _table = dbContext.Set<T>();
        }
        public virtual async Task<T?> FindAsync(int id) => await _table.FindAsync(id);
        public virtual async Task<IEnumerable<T>> FindAllAsync() => await _table.ToListAsync();
        public virtual async Task<IEnumerable<T>> FindAllAsync(params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _table;
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
            return await query.ToListAsync();
        }
        public virtual async Task<IEnumerable<T>> FindByConditionAsync(Expression<Func<T, bool>> expression, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _table.Where(expression);
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
            return await query.ToListAsync();
        }
        public virtual async Task<IEnumerable<T>> FindByConditionAsync(Expression<Func<T, bool>> expression)
        {
            return await _table.Where(expression).ToListAsync();
        }
        public virtual async Task<T> AddAsync(T entity)
        {
            _dbContext.Add(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }
        public virtual async Task<T?> DeleteAsync(T entity)
        {
            if (entity != null)
            {
                _table.Remove(entity);
                await _dbContext.SaveChangesAsync();
                return entity;
            }
            return null;
        }
        public virtual async Task<T?> UpdateAsync(T entity)
        {
            if (entity != null)
            {
                try
                {
                    var existingEntity = await _table.FindAsync(entity.Id);
                    if (existingEntity == null)
                    {
                        return null;// Entity not found
                    }
                    _dbContext.Entry(existingEntity).CurrentValues.SetValues(entity);
                    await _dbContext.SaveChangesAsync();
                    return existingEntity;
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Entity has been modified or deleted since it was loaded
                    var existingEntity = await _table.FindAsync(entity.Id);
                    if (existingEntity == null)
                    {
                        return null;// Entity was deleted
                    }
                    throw; // Rethrow if it's a genuine concurrency issue
                }
            }
            return null;
        }
        public virtual async Task<int> GetCountAsync() => await _table.CountAsync();
    }
}
