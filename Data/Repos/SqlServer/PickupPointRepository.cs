using Data.Contexts.SqlServer;
using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using Microsoft.EntityFrameworkCore;
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

        public async Task<List<(PickupPoint PickupPoint, int AssignedStudentCount)>> GetPickupPointsWithStudentCountAsync()
        {
            var pickupPoints = await _dbContext.Set<PickupPoint>()
                .Where(pp => !pp.IsDeleted && 
                           _dbContext.Set<Student>()
                               .Any(s => !s.IsDeleted && s.CurrentPickupPointId == pp.Id && s.Status == StudentStatus.Active))
                .OrderBy(pp => pp.Description)
                .ToListAsync();

            var result = new List<(PickupPoint PickupPoint, int AssignedStudentCount)>();

            foreach (var pickupPoint in pickupPoints)
            {
                var assignedStudentCount = await _dbContext.Set<Student>()
                    .CountAsync(s => !s.IsDeleted && s.CurrentPickupPointId == pickupPoint.Id && s.Status == StudentStatus.Active);

                result.Add((pickupPoint, assignedStudentCount));
            }

            return result;
        }
    }
}
