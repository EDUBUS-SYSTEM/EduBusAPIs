using Services.Models.RelocationRequest;

namespace Services.Contracts
{
	public interface IRelocationRequestService
	{
		// Parent operations
		Task<RelocationRequestResponseDto> CreateRequestAsync(
			CreateRelocationRequestDto dto,
			Guid parentId);

		Task<RelocationRequestListResponse> GetMyRequestsAsync(
			Guid parentId,
			string? status = null,
			int page = 1,
			int perPage = 20);

		Task<RelocationRequestResponseDto?> GetRequestByIdAsync(Guid requestId);

		Task<RefundCalculationResult> CalculateRefundPreviewAsync(
			Guid studentId,
			double newDistanceKm);

		// Admin operations
		Task<RelocationRequestListResponse> GetAllRequestsAsync(
			string? status = null,
			string? semesterCode = null,
			DateTime? fromDate = null,
			DateTime? toDate = null,
			int page = 1,
			int perPage = 20);

		Task<RelocationRequestResponseDto> ApproveRequestAsync(
			Guid requestId,
			ApproveRelocationRequestDto dto,
			Guid adminId);

		Task<RelocationRequestResponseDto> RejectRequestAsync(
			Guid requestId,
			RejectRelocationRequestDto dto,
			Guid adminId);
	}
}