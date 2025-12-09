namespace Data.Models;
public class FaceEmbedding : BaseDomain
{
    public Guid StudentId { get; set; }

    public string EmbeddingJson { get; set; } = null!;

    public string ModelVersion { get; set; } = Constants.TripConstants.FaceRecognitionConstants.ModelVersions.MobileFaceNet_V1;

    public Guid? FirstPhotoFileId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public virtual Student Student { get; set; } = null!;
}
