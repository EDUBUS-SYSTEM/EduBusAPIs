using Data.Models;
using Data.Repos.Interfaces;
using MongoDB.Driver;

namespace Data.Repos.MongoDB
{
	public class RelocationRequestRepository : IRelocationRequestRepository
	{
		private readonly IMongoCollection<RelocationRequestDocument> _collection;

		public RelocationRequestRepository(IMongoDatabase database)
		{
			_collection = database.GetCollection<RelocationRequestDocument>("RelocationRequests");

			// Create indexes
			CreateIndexes();
		}

		private void CreateIndexes()
		{
			var indexKeys = Builders<RelocationRequestDocument>.IndexKeys;

			// Index on ParentId
			_collection.Indexes.CreateOne(
				new CreateIndexModel<RelocationRequestDocument>(
					indexKeys.Ascending(x => x.ParentId)));

			// Index on StudentId
			_collection.Indexes.CreateOne(
				new CreateIndexModel<RelocationRequestDocument>(
					indexKeys.Ascending(x => x.StudentId)));

			// Index on RequestStatus
			_collection.Indexes.CreateOne(
				new CreateIndexModel<RelocationRequestDocument>(
					indexKeys.Ascending(x => x.RequestStatus)));

			// Index on SemesterCode
			_collection.Indexes.CreateOne(
				new CreateIndexModel<RelocationRequestDocument>(
					indexKeys.Ascending(x => x.SemesterCode)));

			// Compound index on StudentId + SemesterCode + RequestStatus
			_collection.Indexes.CreateOne(
				new CreateIndexModel<RelocationRequestDocument>(
					indexKeys
						.Ascending(x => x.StudentId)
						.Ascending(x => x.SemesterCode)
						.Ascending(x => x.RequestStatus)));
		}

		public async Task<RelocationRequestDocument> AddAsync(RelocationRequestDocument document)
		{
			document.CreatedAt = DateTime.UtcNow;
			document.SubmittedAt = DateTime.UtcNow;
			document.LastStatusUpdate = DateTime.UtcNow;

			await _collection.InsertOneAsync(document);
			return document;
		}

		public async Task<RelocationRequestDocument?> FindAsync(Guid id)
		{
			return await _collection
				.Find(x => x.Id == id && !x.IsDeleted)
				.FirstOrDefaultAsync();
		}

		public async Task<RelocationRequestDocument> UpdateAsync(RelocationRequestDocument document)
		{
			document.UpdatedAt = DateTime.UtcNow;
			document.LastStatusUpdate = DateTime.UtcNow;

			await _collection.ReplaceOneAsync(
				x => x.Id == document.Id,
				document);

			return document;
		}

		public async Task DeleteAsync(Guid id)
		{
			var update = Builders<RelocationRequestDocument>.Update
				.Set(x => x.IsDeleted, true)
				.Set(x => x.UpdatedAt, DateTime.UtcNow);

			await _collection.UpdateOneAsync(x => x.Id == id, update);
		}

		public async Task<List<RelocationRequestDocument>> GetByParentIdAsync(
			Guid parentId,
			string? status = null,
			int skip = 0,
			int take = 20)
		{
			var filter = Builders<RelocationRequestDocument>.Filter.And(
				Builders<RelocationRequestDocument>.Filter.Eq(x => x.ParentId, parentId),
				Builders<RelocationRequestDocument>.Filter.Eq(x => x.IsDeleted, false));

			if (!string.IsNullOrWhiteSpace(status))
			{
				filter = Builders<RelocationRequestDocument>.Filter.And(
					filter,
					Builders<RelocationRequestDocument>.Filter.Eq(x => x.RequestStatus, status));
			}

			return await _collection
				.Find(filter)
				.SortByDescending(x => x.SubmittedAt)
				.Skip(skip)
				.Limit(take)
				.ToListAsync();
		}

		public async Task<List<RelocationRequestDocument>> GetByStudentIdAsync(
			Guid studentId,
			string? status = null,
			int skip = 0,
			int take = 20)
		{
			var filter = Builders<RelocationRequestDocument>.Filter.And(
				Builders<RelocationRequestDocument>.Filter.Eq(x => x.StudentId, studentId),
				Builders<RelocationRequestDocument>.Filter.Eq(x => x.IsDeleted, false));

			if (!string.IsNullOrWhiteSpace(status))
			{
				filter = Builders<RelocationRequestDocument>.Filter.And(
					filter,
					Builders<RelocationRequestDocument>.Filter.Eq(x => x.RequestStatus, status));
			}

			return await _collection
				.Find(filter)
				.SortByDescending(x => x.SubmittedAt)
				.Skip(skip)
				.Limit(take)
				.ToListAsync();
		}

		public async Task<List<RelocationRequestDocument>> GetAllAsync(
			string? status = null,
			string? semesterCode = null,
			DateTime? fromDate = null,
			DateTime? toDate = null,
			int skip = 0,
			int take = 20)
		{
			var filterBuilder = Builders<RelocationRequestDocument>.Filter;
			var filter = filterBuilder.Eq(x => x.IsDeleted, false);

			if (!string.IsNullOrWhiteSpace(status))
			{
				filter = filterBuilder.And(filter, filterBuilder.Eq(x => x.RequestStatus, status));
			}

			if (!string.IsNullOrWhiteSpace(semesterCode))
			{
				filter = filterBuilder.And(filter, filterBuilder.Eq(x => x.SemesterCode, semesterCode));
			}

			if (fromDate.HasValue)
			{
				filter = filterBuilder.And(filter, filterBuilder.Gte(x => x.SubmittedAt, fromDate.Value));
			}

			if (toDate.HasValue)
			{
				filter = filterBuilder.And(filter, filterBuilder.Lte(x => x.SubmittedAt, toDate.Value));
			}

			return await _collection
				.Find(filter)
				.SortByDescending(x => x.SubmittedAt)
				.Skip(skip)
				.Limit(take)
				.ToListAsync();
		}

		public async Task<RelocationRequestDocument?> GetPendingRequestForStudentAsync(
			Guid studentId,
			string semesterCode)
		{
			return await _collection
				.Find(x => x.StudentId == studentId
					&& x.SemesterCode == semesterCode
					&& x.RequestStatus == RelocationRequestStatus.Pending
					&& !x.IsDeleted)
				.FirstOrDefaultAsync();
		}

		public async Task<int> CountByStatusAsync(string status)
		{
			return (int)await _collection.CountDocumentsAsync(
				x => x.RequestStatus == status && !x.IsDeleted);
		}
	}
}