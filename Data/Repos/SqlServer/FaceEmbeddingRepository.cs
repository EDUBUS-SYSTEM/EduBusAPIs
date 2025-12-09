using Data.Models;
using Data.Repos.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Data.Repos.SqlServer
{
	public class FaceEmbeddingRepository : SqlRepository<FaceEmbedding>, IFaceEmbeddingRepository
	{
		public FaceEmbeddingRepository(DbContext dbContext) : base(dbContext)
		{
		}

		public async Task<FaceEmbedding?> GetByStudentIdAsync(Guid studentId)
		{
			return await _table
				.FirstOrDefaultAsync(f => f.StudentId == studentId && !f.IsDeleted);
		}
	}
}
