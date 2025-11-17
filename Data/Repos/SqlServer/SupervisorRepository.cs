using Data.Models;
using Data.Repos.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Data.Repos.SqlServer
{
    public class SupervisorRepository : SqlRepository<Supervisor>, ISupervisorRepository
    {
        public SupervisorRepository(DbContext dbContext) : base(dbContext)
        {
        }
    }
}

