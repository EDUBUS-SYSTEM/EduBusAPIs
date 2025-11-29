namespace Constants
{
    public static class TripConstants
    {
        public static class TripStatus
        {
            public const string Scheduled = "Scheduled";
            public const string InProgress = "InProgress";
            public const string Completed = "Completed";
            public const string Cancelled = "Cancelled";
            public const string Delayed = "Delayed";
            public const string OnTime = "OnTime";
            public const string Early = "Early";
            public const string Late = "Late";
        }

        public static class TripStatusTransitions
        {
            private static readonly Dictionary<string, HashSet<string>> _allowedTransitions = new()
            {
                { TripStatus.Scheduled, new HashSet<string> { TripStatus.InProgress, TripStatus.Cancelled, TripStatus.Delayed } },
                { TripStatus.InProgress, new HashSet<string> { TripStatus.Completed, TripStatus.Cancelled, TripStatus.Delayed } },
                { TripStatus.Delayed, new HashSet<string> { TripStatus.InProgress, TripStatus.Completed, TripStatus.Cancelled } },
                { TripStatus.Completed, new HashSet<string>() }, // Terminal state
                { TripStatus.Cancelled, new HashSet<string>() }  // Terminal state
            };

            public static bool IsValidTransition(string fromStatus, string toStatus)
            {
                if (string.IsNullOrEmpty(fromStatus) || string.IsNullOrEmpty(toStatus))
                    return false;

                return _allowedTransitions.ContainsKey(fromStatus) && 
                       _allowedTransitions[fromStatus].Contains(toStatus);
            }

            public static IEnumerable<string> GetAllowedTransitions(string fromStatus)
            {
                if (string.IsNullOrEmpty(fromStatus) || !_allowedTransitions.ContainsKey(fromStatus))
                    return Enumerable.Empty<string>();

                return _allowedTransitions[fromStatus];
            }
        }

        public static class TripOverrideTypes
        {
            public const string TimeChange = "TIME_CHANGE";
            public const string Cancellation = "CANCELLATION";
            public const string Delay = "DELAY";
            public const string RouteChange = "ROUTE_CHANGE";
            public const string Emergency = "EMERGENCY";
        }

        public static class AttendanceStates
        {
            public const string Present = "Present";
            public const string Absent = "Absent";
            public const string Late = "Late";
            public const string Excused = "Excused";
            public const string Pending = "Pending";
        }

        public static class FaceRecognitionConstants
        {
            public static class ModelVersions
            {
                public const string MobileFaceNet_V1 = "mobilefacenet_v1";
            }

            public static class RecognitionMethods
            {
                public const string Manual = "Manual";
                public const string FaceRecognition = "FaceRecognition";
            }
        }
    }
}

