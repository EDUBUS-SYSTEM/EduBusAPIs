using System.ComponentModel.DataAnnotations;

namespace Services.Models.Parent
{
    public class ParentTripReportRequest
    {
        [Required]
        public string SemesterId { get; set; } = string.Empty;
    }
}

