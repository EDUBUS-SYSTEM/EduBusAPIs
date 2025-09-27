using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Utils
{
    public static class RRuleHelper
    {
        public static bool IsDateMatchingRRule(DateTime date, string rrule)
        {
            if (string.IsNullOrWhiteSpace(rrule))
                return true; // Empty RRule treated as DAILY

            var parts = ParseRRule(rrule);
            var freq = parts.GetValueOrDefault("FREQ", "").ToUpperInvariant();

            return freq switch
            {
                "DAILY" => IsDateMatchingDaily(date, parts),
                "WEEKLY" => IsDateMatchingWeekly(date, parts),
                "MONTHLY" => IsDateMatchingMonthly(date, parts),
                "YEARLY" => IsDateMatchingYearly(date, parts),
                _ => false
            };
        }

        public static List<DateTime> GenerateDatesFromRRule(string rrule, DateTime startDate, DateTime endDate)
        {
            var dates = new List<DateTime>();
            if (string.IsNullOrWhiteSpace(rrule))
            {
                // Empty RRule treated as DAILY
                for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
                {
                    dates.Add(date);
                }
                return dates;
            }

            var parts = ParseRRule(rrule);
            var freq = parts.GetValueOrDefault("FREQ", "").ToUpperInvariant();
            var interval = int.Parse(parts.GetValueOrDefault("INTERVAL", "1"));

            var currentDate = startDate.Date;
            while (currentDate <= endDate.Date)
            {
                if (IsDateMatchingRRule(currentDate, rrule))
                {
                    dates.Add(currentDate);
                }

                currentDate = freq switch
                {
                    "DAILY" => currentDate.AddDays(interval),
                    "WEEKLY" => currentDate.AddDays(7 * interval),
                    "MONTHLY" => currentDate.AddMonths(interval),
                    "YEARLY" => currentDate.AddYears(interval),
                    _ => currentDate.AddDays(1)
                };
            }

            return dates;
        }

        public static bool ValidateEnhancedRRule(string rrule)
        {
            if (string.IsNullOrWhiteSpace(rrule))
                return true; // Empty RRule is valid

            try
            {
                var parts = ParseRRule(rrule);
                var freq = parts.GetValueOrDefault("FREQ", "").ToUpperInvariant();

                // Validate FREQ
                if (!new[] { "DAILY", "WEEKLY", "MONTHLY", "YEARLY" }.Contains(freq))
                    return false;

                // Validate INTERVAL
                if (parts.ContainsKey("INTERVAL"))
                {
                    if (!int.TryParse(parts["INTERVAL"], out var interval) || interval < 1)
                        return false;
                }

                // Validate BYDAY for WEEKLY
                if (freq == "WEEKLY" && parts.ContainsKey("BYDAY"))
                {
                    var validDays = new[] { "MO", "TU", "WE", "TH", "FR", "SA", "SU" };
                    var days = parts["BYDAY"].Split(',', StringSplitOptions.RemoveEmptyEntries);
                    if (!days.All(d => validDays.Contains(d.Trim().ToUpperInvariant())))
                        return false;
                }

                // Validate BYMONTHDAY for MONTHLY
                if (freq == "MONTHLY" && parts.ContainsKey("BYMONTHDAY"))
                {
                    var days = parts["BYMONTHDAY"].Split(',', StringSplitOptions.RemoveEmptyEntries);
                    if (!days.All(d => int.TryParse(d.Trim(), out var day) && day >= 1 && day <= 31))
                        return false;
                }

                // Validate BYMONTH for YEARLY
                if (freq == "YEARLY" && parts.ContainsKey("BYMONTH"))
                {
                    var months = parts["BYMONTH"].Split(',', StringSplitOptions.RemoveEmptyEntries);
                    if (!months.All(m => int.TryParse(m.Trim(), out var month) && month >= 1 && month <= 12))
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static Dictionary<string, string> ParseRRule(string rrule)
        {
            var parts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var segments = rrule.Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (var segment in segments)
            {
                var kv = segment.Split('=', 2);
                if (kv.Length == 2)
                {
                    parts[kv[0].Trim()] = kv[1].Trim();
                }
            }

            return parts;
        }

        private static bool IsDateMatchingDaily(DateTime date, Dictionary<string, string> parts)
        {
            var interval = int.Parse(parts.GetValueOrDefault("INTERVAL", "1"));
            // For DAILY, we just check if it's within the interval
            return true; // The interval is handled in the main loop
        }

        private static bool IsDateMatchingWeekly(DateTime date, Dictionary<string, string> parts)
        {
            if (!parts.ContainsKey("BYDAY"))
                return true; // All days of the week

            var validDays = new[] { "MO", "TU", "WE", "TH", "FR", "SA", "SU" };
            var dayOfWeek = date.DayOfWeek switch
            {
                DayOfWeek.Monday => "MO",
                DayOfWeek.Tuesday => "TU",
                DayOfWeek.Wednesday => "WE",
                DayOfWeek.Thursday => "TH",
                DayOfWeek.Friday => "FR",
                DayOfWeek.Saturday => "SA",
                DayOfWeek.Sunday => "SU",
                _ => ""
            };

            var allowedDays = parts["BYDAY"].Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(d => d.Trim().ToUpperInvariant())
                .ToHashSet();

            return allowedDays.Contains(dayOfWeek);
        }

        private static bool IsDateMatchingMonthly(DateTime date, Dictionary<string, string> parts)
        {
            if (!parts.ContainsKey("BYMONTHDAY"))
                return true; // All days of the month

            var allowedDays = parts["BYMONTHDAY"].Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(d => int.Parse(d.Trim()))
                .ToHashSet();

            return allowedDays.Contains(date.Day);
        }

        private static bool IsDateMatchingYearly(DateTime date, Dictionary<string, string> parts)
        {
            var monthMatch = true;
            var dayMatch = true;

            if (parts.ContainsKey("BYMONTH"))
            {
                var allowedMonths = parts["BYMONTH"].Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(m => int.Parse(m.Trim()))
                    .ToHashSet();
                monthMatch = allowedMonths.Contains(date.Month);
            }

            if (parts.ContainsKey("BYMONTHDAY"))
            {
                var allowedDays = parts["BYMONTHDAY"].Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(d => int.Parse(d.Trim()))
                    .ToHashSet();
                dayMatch = allowedDays.Contains(date.Day);
            }

            return monthMatch && dayMatch;
        }

        public static Dictionary<string, string> GetSupportedPatterns()
        {
            return new Dictionary<string, string>
            {
                ["DAILY"] = "FREQ=DAILY;INTERVAL=1",
                ["WEEKLY"] = "FREQ=WEEKLY;BYDAY=MO,TU,WE,TH,FR",
                ["MONTHLY"] = "FREQ=MONTHLY;BYMONTHDAY=1,15",
                ["YEARLY"] = "FREQ=YEARLY;BYMONTH=9;BYMONTHDAY=1",
                ["COMPLEX"] = "FREQ=WEEKLY;BYDAY=MO,TU,WE,TH,FR;BYMONTH=9,10,11,12,1,2,3,4,5"
            };
        }
    }
}
