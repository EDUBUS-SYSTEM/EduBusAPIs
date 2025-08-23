using Data.Models;
using Data.Repos.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repos.SqlServer
{
    public class DriverRepository : SqlRepository<Driver>, IDriverRepository
    {
        public DriverRepository(DbContext dbContext) : base(dbContext)
        {
        }
    }
}
