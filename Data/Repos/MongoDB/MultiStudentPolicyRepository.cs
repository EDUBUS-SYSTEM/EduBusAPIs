using Data.Models;
using Data.Repos.Interfaces;
using MongoDB.Driver;

namespace Data.Repos.MongoDB;

public class MultiStudentPolicyRepository : MongoRepository<MultiStudentPolicyDocument>, IMultiStudentPolicyRepository
{
    public MultiStudentPolicyRepository(IMongoDatabase db)
        : base(db, "multistudentpolicydocument") { }

    public async Task<List<MultiStudentPolicyDocument>> GetActivePoliciesAsync()
    {
        var filter = Builders<MultiStudentPolicyDocument>.Filter.And(
            Builders<MultiStudentPolicyDocument>.Filter.Eq(x => x.IsDeleted, false),
            Builders<MultiStudentPolicyDocument>.Filter.Eq(x => x.IsActive, true)
        );

        var sort = Builders<MultiStudentPolicyDocument>.Sort.Descending(x => x.CreatedAt);
        return await _collection.Find(filter).Sort(sort).ToListAsync();
    }

    public async Task<MultiStudentPolicyDocument?> GetCurrentActivePolicyAsync()
    {
        var now = DateTime.UtcNow;
        var filter = Builders<MultiStudentPolicyDocument>.Filter.And(
            Builders<MultiStudentPolicyDocument>.Filter.Eq(x => x.IsDeleted, false),
            Builders<MultiStudentPolicyDocument>.Filter.Eq(x => x.IsActive, true),
            Builders<MultiStudentPolicyDocument>.Filter.Lte(x => x.EffectiveFrom, now),
            Builders<MultiStudentPolicyDocument>.Filter.Or(
                Builders<MultiStudentPolicyDocument>.Filter.Eq(x => x.EffectiveTo, null),
                Builders<MultiStudentPolicyDocument>.Filter.Gte(x => x.EffectiveTo, now)
            )
        );

        var sort = Builders<MultiStudentPolicyDocument>.Sort.Descending(x => x.CreatedAt);
        return await _collection.Find(filter).Sort(sort).FirstOrDefaultAsync();
    }

    public async Task<MultiStudentPolicyDocument?> GetEffectivePolicyAsync(DateTime date)
    {
        var filter = Builders<MultiStudentPolicyDocument>.Filter.And(
            Builders<MultiStudentPolicyDocument>.Filter.Eq(x => x.IsDeleted, false),
            Builders<MultiStudentPolicyDocument>.Filter.Eq(x => x.IsActive, true),
            Builders<MultiStudentPolicyDocument>.Filter.Lte(x => x.EffectiveFrom, date),
            Builders<MultiStudentPolicyDocument>.Filter.Or(
                Builders<MultiStudentPolicyDocument>.Filter.Eq(x => x.EffectiveTo, null),
                Builders<MultiStudentPolicyDocument>.Filter.Gte(x => x.EffectiveTo, date)
            )
        );

        var sort = Builders<MultiStudentPolicyDocument>.Sort.Descending(x => x.CreatedAt);
        return await _collection.Find(filter).Sort(sort).FirstOrDefaultAsync();
    }

    public async Task<List<MultiStudentPolicyDocument>> GetAllIncludingDeletedAsync()
    {
        var sort = Builders<MultiStudentPolicyDocument>.Sort.Descending(x => x.CreatedAt);
        return await _collection.Find(_ => true).Sort(sort).ToListAsync();
    }
}

