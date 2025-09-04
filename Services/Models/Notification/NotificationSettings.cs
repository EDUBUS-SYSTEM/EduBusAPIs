namespace Services.Models.Notification
{
    public class NotificationSettings
    {
        public AutoReplacementSuggestionSettings AutoReplacementSuggestion { get; set; } = new();
        public NotificationCleanupSettings NotificationCleanup { get; set; } = new();
        public Dictionary<string, int> DefaultExpirationDays { get; set; } = new();
        public Dictionary<string, int> PrioritySettings { get; set; } = new();
    }

    public class AutoReplacementSuggestionSettings
    {
        public int CheckIntervalMinutes { get; set; } = 15;
        public int ProcessingWindowHours { get; set; } = 24;
        public int MaxFutureDays { get; set; } = 7;
        public int CooldownHours { get; set; } = 2;
    }

    public class NotificationCleanupSettings
    {
        public int CleanupIntervalHours { get; set; } = 6;
        public int DefaultExpirationDays { get; set; } = 30;
    }
}
