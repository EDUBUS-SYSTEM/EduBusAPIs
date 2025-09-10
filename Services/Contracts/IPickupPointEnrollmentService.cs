﻿using Services.Models.PickupPoint;

namespace Services.Contracts
{
    public interface IPickupPointEnrollmentService
    {
        Task<ParentRegistrationResponseDto> RegisterParentAsync(ParentRegistrationRequestDto dto);
        Task<VerifyOtpWithStudentsResponseDto> VerifyOtpWithStudentsAsync(string email, string otp);
        Task<List<StudentBriefDto>> GetStudentsByEmailAsync(string email);

        Task<SubmitPickupPointRequestResponseDto> SubmitPickupPointRequestAsync(SubmitPickupPointRequestDto dto);

        Task<List<PickupPointRequestDetailDto>> ListRequestDetailsAsync(PickupPointRequestListQuery query);
        Task ApproveRequestAsync(Guid requestId, Guid adminId, string? notes);
        Task RejectRequestAsync(Guid requestId, Guid adminId, string reason);
    }
}
