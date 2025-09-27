using Data.Contexts.SqlServer;
using Data.Models;
using Data.Repos.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Data.Repos.SqlServer;

public class UnitPriceRepository : SqlRepository<UnitPrice>, IUnitPriceRepository
{
    public UnitPriceRepository(EduBusSqlContext context) : base(context)
    {
    }

    public async Task<List<UnitPrice>> GetActiveUnitPricesAsync()
    {
        return await _table
            .Where(up => up.IsActive && !up.IsDeleted)
            .OrderByDescending(up => up.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<UnitPrice>> GetEffectiveUnitPricesAsync(DateTime date)
    {
        return await _table
            .Where(up => up.IsActive &&
                        !up.IsDeleted &&
                        up.EffectiveFrom <= date &&
                        (up.EffectiveTo == null || up.EffectiveTo >= date))
            .OrderByDescending(up => up.CreatedAt)
            .ToListAsync();
    }

    public async Task<UnitPrice?> GetCurrentEffectiveUnitPriceAsync()
    {
        return await _table
            .Where(up => up.IsActive &&
                        !up.IsDeleted &&
                        up.EffectiveFrom <= DateTime.UtcNow &&
                        (up.EffectiveTo == null || up.EffectiveTo >= DateTime.UtcNow))
            .OrderByDescending(up => up.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<UnitPrice>> GetAllIncludingDeletedAsync()
    {
        return await _table
            .OrderByDescending(up => up.CreatedAt)
            .ToListAsync();
    }
}
