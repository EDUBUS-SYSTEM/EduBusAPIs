using Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repos.Interfaces
{
    public interface IStudentRepository: ISqlRepository<Student>
    {
        Task<List<Student>> GetStudentsByParentEmailAsync(string email);
        Task<List<Student>> GetActiveStudentsByPickupPointIdAsync(Guid pickupPointId);
    }
}
