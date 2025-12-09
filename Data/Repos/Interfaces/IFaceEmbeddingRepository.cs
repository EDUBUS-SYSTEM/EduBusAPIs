using Data.Models;

namespace Data.Repos.Interfaces
{
	public interface IFaceEmbeddingRepository : ISqlRepository<FaceEmbedding>
	{
		Task<FaceEmbedding?> GetByStudentIdAsync(Guid studentId);
	}
}
