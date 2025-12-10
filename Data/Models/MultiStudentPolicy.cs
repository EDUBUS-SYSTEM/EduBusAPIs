using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace Data.Models;

/// <summary>
/// Multi-student policy document stored in MongoDB
/// </summary>
public class MultiStudentPolicyDocument : BaseMongoDocument
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("effectiveFrom")]
    public DateTime EffectiveFrom { get; set; }

    [BsonElement("effectiveTo")]
    public DateTime? EffectiveTo { get; set; }

    [BsonElement("tiers")]
    public List<MultiStudentPolicyTierDocument> Tiers { get; set; } = new();

    [BsonElement("byAdminId")]
    public Guid ByAdminId { get; set; }

    [BsonElement("byAdminName")]
    public string ByAdminName { get; set; } = string.Empty;
}

/// <summary>
/// Policy tier embedded document (stored within policy document)
/// </summary>
public class MultiStudentPolicyTierDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.Binary)]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [BsonElement("minStudentCount")]
    public int MinStudentCount { get; set; }

    [BsonElement("maxStudentCount")]
    public int? MaxStudentCount { get; set; }

    [BsonElement("reductionPercentage")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal ReductionPercentage { get; set; }

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("priority")]
    public int Priority { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; } = false;
}
