namespace Services.Models.DriverVehicle
{
    public class AssignmentConflictResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<AssignmentConflictDto> Conflicts { get; set; } = new List<AssignmentConflictDto>();
        public int TotalConflicts { get; set; }
        public int CriticalConflicts { get; set; }
        public int HighConflicts { get; set; }
        public int MediumConflicts { get; set; }
        public int LowConflicts { get; set; }
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
        
        // Conflict summary
        public bool HasConflicts => Conflicts.Any();
        public bool HasCriticalConflicts => CriticalConflicts > 0;
        public bool HasHighConflicts => HighConflicts > 0;
        
        // Resolution suggestions
        public List<string> GeneralResolutionSuggestions { get; set; } = new List<string>();
        public bool IsResolvable { get; set; }
        public string? ResolutionNotes { get; set; }
    }
}

