using Data.Models;
using Data.Repos.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Linq.Expressions;

namespace Data.Repos.SqlServer
{
    public class SqlRepository<T> : ISqlRepository<T> where T : BaseDomain
    {
        protected readonly DbContext _dbContext;
        protected readonly DbSet<T> _table;
        public DatabaseFacade Database => _dbContext.Database;

        public SqlRepository(DbContext dbContext)
        {
            _dbContext = dbContext;
            _table = dbContext.Set<T>();
        }

        public virtual IQueryable<T> GetQueryable() => _table.AsQueryable();

        public virtual async Task<T?> FindAsync(Guid id) => await _table.FindAsync(id);

        public virtual async Task<IEnumerable<T>> FindAllAsync() => await _table.Where(e => !e.IsDeleted).ToListAsync();

        public virtual async Task<IEnumerable<T>> FindAllAsync(params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _table.Where(e => !e.IsDeleted);
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
            return await query.ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> FindByConditionAsync(Expression<Func<T, bool>> expression)
        {
            return await _table.Where(e => !e.IsDeleted).Where(expression).ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> FindByConditionAsync(Expression<Func<T, bool>> expression, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _table.Where(e => !e.IsDeleted).Where(expression);
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
            return await query.ToListAsync();
        }

        public virtual async Task<T> AddAsync(T entity)
        {
            entity.CreatedAt = DateTime.UtcNow;
            _dbContext.Add(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }

        public virtual async Task<T?> UpdateAsync(T entity)
        {
            if (entity != null)
            {
                try
                {
                    var existingEntity = await _table.FindAsync(entity.Id);
                    if (existingEntity == null || existingEntity.IsDeleted)
                    {
                        return null; // Entity not found or deleted
                    }
                    
                    entity.UpdatedAt = DateTime.UtcNow;
                    _dbContext.Entry(existingEntity).CurrentValues.SetValues(entity);
                    await _dbContext.SaveChangesAsync();
                    return existingEntity;
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Entity has been modified or deleted since it was loaded
                    var existingEntity = await _table.FindAsync(entity.Id);
                    if (existingEntity == null || existingEntity.IsDeleted)
                    {
                        return null; // Entity was deleted
                    }
                    throw; // Rethrow if it's a genuine concurrency issue
                }
            }
            return null;
        }

        public virtual async Task<T?> DeleteAsync(T entity)
        {
            if (entity != null)
            {
                entity.IsDeleted = true;
                entity.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
                return entity;
            }
            return null;
        }

        public virtual async Task<int> GetCountAsync() => await _table.Where(e => !e.IsDeleted).CountAsync();

        public virtual async Task<bool> ExistsAsync(Guid id)
        {
            return await _table.AnyAsync(e => e.Id == id && !e.IsDeleted);
        }
    }
}
