using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Services.Models.UserAccount;

namespace Services.Contracts
{
    public interface IUserService
    {
        Task<UserListResponse> GetUsersAsync(string? status, string? role, string? search, int page, int perPage, string? sortBy, string? sortOrder);
        Task<UserResponse?> GetUserByIdAsync(Guid userId);
        Task<UserResponse> UpdateUserAsync(Guid userId, UserUpdateRequest request);
        Task<UserResponse> PartialUpdateUserAsync(Guid userId, UserPartialUpdateRequest request);
        Task<BasicSuccessResponse> DeleteUserAsync(Guid userId);

		// Account locking methods
		Task<BasicSuccessResponse> LockUserAsync(Guid userId, DateTime? lockedUntil, string? reason, Guid lockedBy);
		Task<BasicSuccessResponse> UnlockUserAsync(Guid userId, Guid unlockedBy);
		Task<BasicSuccessResponse> LockMultipleUsersAsync(List<Guid> userIds, DateTime? lockedUntil, string? reason, Guid lockedBy);
		Task<BasicSuccessResponse> UnlockMultipleUsersAsync(List<Guid> userIds, Guid unlockedBy);
	}
}
