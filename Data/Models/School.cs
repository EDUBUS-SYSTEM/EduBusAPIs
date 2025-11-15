using NetTopologySuite.Geometries;

namespace Data.Models;

public partial class School : BaseDomain
{
    public string SchoolName { get; set; } = null!;
    public string? Slogan { get; set; }
    public string? ShortDescription { get; set; } 
    public string? FullDescription { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Website { get; set; }
    public string? FullAddress { get; set; }
    public string? DisplayAddress { get; set; } 
    public Point? Geog { get; set; } 
    public double? Latitude { get; set; }
    public double? Longitude { get; set; } 
    public string? FooterText { get; set; }
    public bool IsPublished { get; set; } = false; 
    public bool IsActive { get; set; } = true;
    public string? InternalNotes { get; set; }
}

