namespace Services.Models.Driver
{
    public class ConflictResolutionResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<DriverLeaveConflictDto> ResolvedConflicts { get; set; } = new List<DriverLeaveConflictDto>();
        public List<DriverLeaveConflictDto> RemainingConflicts { get; set; } = new List<DriverLeaveConflictDto>();
        public int TotalConflicts { get; set; }
        public int ResolvedCount { get; set; }
        public DateTime ResolvedAt { get; set; } = DateTime.UtcNow;
        public string? ResolutionNotes { get; set; }
    }
}
