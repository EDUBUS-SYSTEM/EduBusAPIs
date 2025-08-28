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
        Task<UserListResponse> GetUsersAsync(string? status, int page, int perPage, string? sortBy, string? sortOrder);
        Task<UserResponse?> GetUserByIdAsync(Guid userId);
        Task<UserResponse> UpdateUserAsync(Guid userId, UserUpdateRequest request);
        Task<UserResponse> PartialUpdateUserAsync(Guid userId, UserPartialUpdateRequest request);
        Task<BasicSuccessResponse> DeleteUserAsync(Guid userId);
    }
}
