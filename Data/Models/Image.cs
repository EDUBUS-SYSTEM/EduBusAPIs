namespace Data.Models;

public partial class Image : BaseDomain
{
    public byte[] HashedUrl { get; set; } = null!;

    public Guid StudentId { get; set; }

    public virtual Student Student { get; set; } = null!;
}
