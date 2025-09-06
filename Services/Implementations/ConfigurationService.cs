using Microsoft.Extensions.Options;
using Services.Contracts;
using Services.Models.Configuration;

namespace Services.Implementations
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly IOptionsMonitor<LeaveRequestSettings> _leaveRequestSettingsMonitor;
        private LeaveRequestSettings _cachedSettings;
        private readonly object _lock = new object();

        public ConfigurationService(IOptionsMonitor<LeaveRequestSettings> leaveRequestSettingsMonitor)
        {
            _leaveRequestSettingsMonitor = leaveRequestSettingsMonitor;
            _cachedSettings = _leaveRequestSettingsMonitor.CurrentValue;
            
            // Subscribe to configuration changes
            _leaveRequestSettingsMonitor.OnChange(settings =>
            {
                lock (_lock)
                {
                    _cachedSettings = settings;
                }
            });
        }

        public LeaveRequestSettings GetLeaveRequestSettings()
        {
            lock (_lock)
            {
                return new LeaveRequestSettings
                {
                    MinimumAdvanceNoticeHours = _cachedSettings.MinimumAdvanceNoticeHours,
                    AllowEmergencyLeaveRequests = _cachedSettings.AllowEmergencyLeaveRequests,
                    EmergencyLeaveAdvanceNoticeHours = _cachedSettings.EmergencyLeaveAdvanceNoticeHours
                };
            }
        }

        public async Task<LeaveRequestSettings> UpdateLeaveRequestSettingsAsync(LeaveRequestSettings settings)
        {
            // Validate settings
            if (settings.MinimumAdvanceNoticeHours < 0)
                throw new ArgumentException("Minimum advance notice hours cannot be negative");
            
            if (settings.EmergencyLeaveAdvanceNoticeHours < 0)
                throw new ArgumentException("Emergency leave advance notice hours cannot be negative");
            
            if (settings.MinimumAdvanceNoticeHours < settings.EmergencyLeaveAdvanceNoticeHours)
                throw new ArgumentException("Minimum advance notice hours cannot be less than emergency leave advance notice hours");

            lock (_lock)
            {
                _cachedSettings.MinimumAdvanceNoticeHours = settings.MinimumAdvanceNoticeHours;
                _cachedSettings.AllowEmergencyLeaveRequests = settings.AllowEmergencyLeaveRequests;
                _cachedSettings.EmergencyLeaveAdvanceNoticeHours = settings.EmergencyLeaveAdvanceNoticeHours;
            }

            return await Task.FromResult(GetLeaveRequestSettings());
        }
    }
}
