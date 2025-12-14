using System.ComponentModel.DataAnnotations;

namespace Utils
{
    public static class FileValidator
    {
        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png" };
        private const long MaxFileSizeBytes = 5 * 1024 * 1024; 
        private const int MaxImageWidthPixels = 2048;
        private const int MaxImageHeightPixels = 2048;

        public static (bool IsValid, string? ErrorMessage) ValidateImageFile(
            string fileName,
            long fileSizeBytes,
            string contentType)
        {
            // Check file size
            if (fileSizeBytes > MaxFileSizeBytes)
            {
                return (false, $"File size exceeds maximum allowed size of {MaxFileSizeBytes / (1024 * 1024)}MB");
            }

            // Check file extension
            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !AllowedImageExtensions.Contains(extension))
            {
                var allowed = string.Join(", ", AllowedImageExtensions);
                return (false, $"Invalid file type. Allowed types: {allowed}");
            }

            // Check content type
            var validContentTypes = new[] { "image/jpeg", "image/jpg", "image/png" };
            if (!validContentTypes.Contains(contentType.ToLowerInvariant()))
            {
                return (false, "Invalid content type. Only JPEG and PNG images are allowed");
            }

            return (true, null);
        }
    }
}
