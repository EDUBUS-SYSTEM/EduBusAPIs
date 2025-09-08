using Services.Models.PickupPoint;
using Data.Models;

namespace Services.Contracts
{
    public interface IPickupPointEnrollmentService
    {
        Task<bool> CheckParentEmailExistsAsync(string email);
        Task SendOtpAsync(string email);
        Task<bool> VerifyOtpAsync(string email, string otp);
        Task<List<StudentBriefDto>> GetStudentsByEmailAsync(string email);

        Task<PickupPointRequestDocument> CreateRequestAsync(CreatePickupPointRequestDto dto);

        Task<List<PickupPointRequestDocument>> ListRequestsAsync(PickupPointRequestListQuery query);
        Task ApproveRequestAsync(Guid requestId, Guid adminId, string? notes);
        Task RejectRequestAsync(Guid requestId, Guid adminId, string reason);
    }
}
