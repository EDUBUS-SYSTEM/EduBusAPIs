namespace Services.Models.Driver
{
    public class ReplacementSuggestionResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<ReplacementSuggestionDto> Suggestions { get; set; } = new List<ReplacementSuggestionDto>();
        public int TotalSuggestions { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public string? AdditionalInfo { get; set; }
    }
}
