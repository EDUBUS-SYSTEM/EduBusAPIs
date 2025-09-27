using Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repos.Interfaces
{
    public interface IUnitPriceRepository : ISqlRepository<UnitPrice>
    {
        Task<List<UnitPrice>> GetActiveUnitPricesAsync();
        Task<List<UnitPrice>> GetEffectiveUnitPricesAsync(DateTime date);
        Task<UnitPrice?> GetCurrentEffectiveUnitPriceAsync();
        Task<List<UnitPrice>> GetAllIncludingDeletedAsync();
    }
}
