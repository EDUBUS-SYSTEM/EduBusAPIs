using Data.Contexts.SqlServer;
using Data.Models;
using Data.Repos.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repos.SqlServer
{
    public class PickupPointRepository : SqlRepository<PickupPoint>, IPickupPointRepository
    {
        public PickupPointRepository(EduBusSqlContext ctx) : base(ctx) { }
    }
}
