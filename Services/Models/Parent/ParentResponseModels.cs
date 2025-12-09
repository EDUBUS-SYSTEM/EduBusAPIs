namespace Services.Models.Parent
{
	public class EnrollChildResponse
	{
		public bool Success { get; set; }
		public string Message { get; set; } = null!;
		public Guid? EmbeddingId { get; set; }
		public int PhotosProcessed { get; set; }
		public double AverageQuality { get; set; }
		public Guid? StudentImageId { get; set; }
	}
}
