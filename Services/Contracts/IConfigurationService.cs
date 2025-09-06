using Services.Models.Configuration;

namespace Services.Contracts
{
    public interface IConfigurationService
    {
        LeaveRequestSettings GetLeaveRequestSettings();
        Task<LeaveRequestSettings> UpdateLeaveRequestSettingsAsync(LeaveRequestSettings settings);
    }
}
