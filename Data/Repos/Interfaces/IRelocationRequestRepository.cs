using Data.Models;

namespace Data.Repos.Interfaces
{
	public interface IRelocationRequestRepository
	{
		Task<RelocationRequestDocument> AddAsync(RelocationRequestDocument document);
		Task<RelocationRequestDocument?> FindAsync(Guid id);
		Task<RelocationRequestDocument> UpdateAsync(RelocationRequestDocument document);
		Task DeleteAsync(Guid id);

		Task<List<RelocationRequestDocument>> GetByParentIdAsync(
			Guid parentId,
			string? status = null,
			int skip = 0,
			int take = 20);

		Task<List<RelocationRequestDocument>> GetByStudentIdAsync(
			Guid studentId,
			string? status = null,
			int skip = 0,
			int take = 20);

		Task<List<RelocationRequestDocument>> GetAllAsync(
			string? status = null,
			string? semesterCode = null,
			DateTime? fromDate = null,
			DateTime? toDate = null,
			int skip = 0,
			int take = 20);

		Task<RelocationRequestDocument?> GetPendingRequestForStudentAsync(
			Guid studentId,
			string semesterCode);

		Task<int> CountByStatusAsync(string status);
	}
}