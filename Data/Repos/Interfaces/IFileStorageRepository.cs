using Data.Models;

namespace Data.Repos.Interfaces
{
    public interface IFileStorageRepository : IMongoRepository<FileStorage>
    {
        Task<FileStorage?> GetActiveFileByEntityAsync(Guid entityId, string entityType, string fileType);
        Task<IEnumerable<FileStorage>> GetFilesByEntityAsync(Guid entityId, string entityType);
        Task<bool> DeactivateFileAsync(Guid fileId);
    }
}
