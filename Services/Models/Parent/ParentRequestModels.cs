using System.ComponentModel.DataAnnotations;

namespace Services.Models.Parent
{
	public class EnrollChildRequest
	{
		[Required(ErrorMessage = "StudentId is required")]
		public Guid StudentId { get; set; }

		[Required(ErrorMessage = "At least 3 face photos are required")]
		[MinLength(3, ErrorMessage = "Minimum 3 photos required")]
		[MaxLength(5, ErrorMessage = "Maximum 5 photos allowed")]
		public List<string> FacePhotos { get; set; } = new();

		public string? Notes { get; set; }
	}
}
