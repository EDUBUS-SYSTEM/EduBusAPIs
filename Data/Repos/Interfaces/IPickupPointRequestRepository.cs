namespace Data.Repos.Interfaces
{
    public interface IPickupPointRequestRepository : IMongoRepository<PickupPointRequestDocument>
    {
        Task<List<PickupPointRequestDocument>> QueryAsync(string? status, string? parentEmail, int skip, int take);
    }
}
