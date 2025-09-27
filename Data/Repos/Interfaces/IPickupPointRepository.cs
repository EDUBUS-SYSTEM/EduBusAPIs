using Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repos.Interfaces
{
    public interface IPickupPointRepository : ISqlRepository<PickupPoint> 
    {
        Task<List<(PickupPoint PickupPoint, int AssignedStudentCount)>> GetPickupPointsWithStudentCountAsync();
    }
}
