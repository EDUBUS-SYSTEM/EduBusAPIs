using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Services.Models.School;

public class SchoolDto
{
    public Guid Id { get; set; }
    public string SchoolName { get; set; } = null!;
    public string? Slogan { get; set; }
    public string? ShortDescription { get; set; }
    public string? FullDescription { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Website { get; set; }
    public string? FullAddress { get; set; }
    public string? DisplayAddress { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? FooterText { get; set; }
    public Guid? LogoFileId { get; set; }
    public string? LogoImageBase64 { get; set; }
    public string? LogoImageContentType { get; set; }
    public SchoolImageContentDto? BannerImage { get; set; }
    public SchoolImageContentDto? StayConnectedImage { get; set; }
    public SchoolImageContentDto? FeatureImage { get; set; }
    public List<SchoolImageContentDto> GalleryImages { get; set; } = new();
    public bool IsPublished { get; set; }
    public bool IsActive { get; set; }
    public string? InternalNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public abstract class SchoolWriteRequestBase
{
    [Required(ErrorMessage = "School name is required.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "School name must be between 1 and 200 characters.")]
    public string SchoolName { get; set; } = null!;

    [StringLength(300, ErrorMessage = "Slogan must not exceed 300 characters.")]
    public string? Slogan { get; set; }

    [StringLength(500, ErrorMessage = "Short description must not exceed 500 characters.")]
    public string? ShortDescription { get; set; }

    [StringLength(5000, ErrorMessage = "Full description must not exceed 5000 characters.")]
    public string? FullDescription { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [StringLength(320, ErrorMessage = "Email must not exceed 320 characters.")]
    public string? Email { get; set; }

    [RegularExpression(@"^[\+]?[(]?[0-9]{1,4}[)]?[-\s\.]?[(]?[0-9]{1,4}[)]?[-\s\.]?[0-9]{1,9}$", 
        ErrorMessage = "Invalid phone number format.")]
    [StringLength(20, ErrorMessage = "Phone number must not exceed 20 characters.")]
    public string? PhoneNumber { get; set; }

    [Url(ErrorMessage = "Invalid website URL format.")]
    [StringLength(500, ErrorMessage = "Website URL must not exceed 500 characters.")]
    public string? Website { get; set; }

    [StringLength(500, ErrorMessage = "Full address must not exceed 500 characters.")]
    public string? FullAddress { get; set; }

    [StringLength(200, ErrorMessage = "Display address must not exceed 200 characters.")]
    public string? DisplayAddress { get; set; }

    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public double? Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    public double? Longitude { get; set; }

    [StringLength(500, ErrorMessage = "Footer text must not exceed 500 characters.")]
    public string? FooterText { get; set; }

    public bool IsPublished { get; set; }

    [StringLength(2000, ErrorMessage = "Internal notes must not exceed 2000 characters.")]
    public string? InternalNotes { get; set; }
}

public class CreateSchoolRequest : SchoolWriteRequestBase
{
}

public class UpdateSchoolRequest : SchoolWriteRequestBase
{
}

public class SchoolLocationRequest
{
    [Required(ErrorMessage = "Latitude is required.")]
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public double Latitude { get; set; }

    [Required(ErrorMessage = "Longitude is required.")]
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    public double Longitude { get; set; }

    [StringLength(500, ErrorMessage = "Full address must not exceed 500 characters.")]
    public string? FullAddress { get; set; }

    [StringLength(200, ErrorMessage = "Display address must not exceed 200 characters.")]
    public string? DisplayAddress { get; set; }
}

public class SchoolImageDto
{
    public Guid FileId { get; set; }
    public string FileType { get; set; } = null!; 
    public string OriginalFileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public DateTime UploadedAt { get; set; }
}

public class SchoolImageContentDto
{
    public Guid FileId { get; set; }
    public string FileType { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public string? Base64Data { get; set; }
    public DateTime UploadedAt { get; set; }
}

