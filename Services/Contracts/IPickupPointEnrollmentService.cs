using Services.Models.PickupPoint;

namespace Services.Contracts
{
    public interface IPickupPointEnrollmentService
    {
        Task<ParentRegistrationResponseDto> RegisterParentAsync(ParentRegistrationRequestDto dto);
        Task<VerifyOtpWithStudentsResponseDto> VerifyOtpWithStudentsAsync(string email, string otp);
        Task<List<StudentBriefDto>> GetStudentsByEmailAsync(string email);
        Task<ParentRegistrationEligibilityDto> GetRegistrationEligibilityAsync(Guid parentId, string? parentEmail);

        Task<SubmitPickupPointRequestResponseDto> SubmitPickupPointRequestAsync(SubmitPickupPointRequestDto dto);

        Task<List<PickupPointRequestDetailDto>> ListRequestDetailsAsync(PickupPointRequestListQuery query);
        Task ApproveRequestAsync(Guid requestId, Guid adminId, string? notes);
        Task RejectRequestAsync(Guid requestId, Guid adminId, string reason);
        
        /// <summary>
        /// Get all pickup points with their assigned student status
        /// </summary>
        Task<List<PickupPointWithStudentStatusDto>> GetPickupPointsWithStudentStatusAsync();
        
        /// <summary>
        /// Assign pickup point to students after successful payment
        /// </summary>
        Task AssignPickupPointAfterPaymentAsync(Guid transactionId);
    }
}
