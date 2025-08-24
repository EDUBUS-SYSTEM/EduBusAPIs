using Data.Models;
using Data.Repos.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Data.Repos.SqlServer
{
    public class DriverLicenseRepository : SqlRepository<DriverLicense>, IDriverLicenseRepository
    {
        public DriverLicenseRepository(DbContext dbContext) : base(dbContext)
        {
        }
    }
}