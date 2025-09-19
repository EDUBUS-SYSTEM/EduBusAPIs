using Data.Models;

namespace Data.Repos.Interfaces
{
    public interface IStudentRepository: ISqlRepository<Student>
    {
        Task<List<Student>> GetStudentsByParentEmailAsync(string email);
    }
}
