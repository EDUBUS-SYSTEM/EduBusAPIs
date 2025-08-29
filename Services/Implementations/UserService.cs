using Data.Repos.Interfaces;
using Services.Contracts;
using Services.Models.UserAccount;
using Data.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

namespace Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly IUserAccountRepository _repository;
        private readonly IMapper _mapper;

        public UserService(IUserAccountRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<UserListResponse> GetUsersAsync(string? status, int page, int perPage, string? sortBy, string? sortOrder)
        {
            // Build query
            var query = _repository.GetQueryable().Where(u => !u.IsDeleted);

            // Apply status filter
            if (!string.IsNullOrEmpty(status))
            {
                if (status.ToLower() == "active")
                {
                    query = query.Where(u => !u.IsDeleted);
                }
                else if (status.ToLower() == "inactive")
                {
                    query = query.Where(u => u.IsDeleted);
                }
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply sorting
            if (!string.IsNullOrEmpty(sortBy))
            {
                var orderDirection = string.IsNullOrEmpty(sortOrder) || sortOrder.ToLower() == "asc" ? "ascending" : "descending";
                query = query.OrderBy($"{sortBy} {orderDirection}");
            }
            else
            {
                // Default sorting by CreatedAt descending
                query = query.OrderByDescending(u => u.CreatedAt);
            }

            // Apply pagination
            var skip = (page - 1) * perPage;
            var users = await query
                .Skip(skip)
                .Take(perPage)
                .ToListAsync();

            // Map to DTOs
            var userDtos = _mapper.Map<List<UserDto>>(users);

            return new UserListResponse
            {
                Users = userDtos,
                TotalCount = totalCount,
                Page = page,
                PerPage = perPage,
                TotalPages = (int)Math.Ceiling((double)totalCount / perPage)
            };
        }

        public async Task<UserResponse?> GetUserByIdAsync(Guid userId)
        {
            var user = await _repository.FindAsync(userId);
            if (user == null || user.IsDeleted)
                return null;

            return _mapper.Map<UserResponse>(user);
        }

        public async Task<UserResponse> UpdateUserAsync(Guid userId, UserUpdateRequest request)
        {
            var user = await _repository.FindAsync(userId);
            if (user == null || user.IsDeleted)
                throw new InvalidOperationException("User not found");

            // Check if email already exists (excluding current user)
            if (await _repository.IsEmailExistAsync(request.Email) && user.Email.ToLower() != request.Email.ToLower())
                throw new InvalidOperationException("Email already exists");

            // Check if phone number already exists (excluding current user)
            if (await _repository.IsPhoneNumberExistAsync(request.PhoneNumber) && user.PhoneNumber != request.PhoneNumber)
                throw new InvalidOperationException("Phone number already exists");

            // Update user properties
            user.Email = request.Email;
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.PhoneNumber = request.PhoneNumber;
            user.Address = request.Address;
            user.DateOfBirth = request.DateOfBirth;
            user.Gender = request.Gender;
            user.UpdatedAt = DateTime.UtcNow;

            var updatedUser = await _repository.UpdateAsync(user);
            if (updatedUser == null)
                throw new InvalidOperationException("Failed to update user");

            return _mapper.Map<UserResponse>(updatedUser);
        }

        public async Task<UserResponse> PartialUpdateUserAsync(Guid userId, UserPartialUpdateRequest request)
        {
            var user = await _repository.FindAsync(userId);
            if (user == null || user.IsDeleted)
                throw new InvalidOperationException("User not found");

            // Update only provided fields
            if (!string.IsNullOrEmpty(request.Email))
            {
                if (await _repository.IsEmailExistAsync(request.Email) && user.Email.ToLower() != request.Email.ToLower())
                    throw new InvalidOperationException("Email already exists");
                user.Email = request.Email;
            }

            if (!string.IsNullOrEmpty(request.FirstName))
                user.FirstName = request.FirstName;

            if (!string.IsNullOrEmpty(request.LastName))
                user.LastName = request.LastName;

            if (!string.IsNullOrEmpty(request.PhoneNumber))
            {
                if (await _repository.IsPhoneNumberExistAsync(request.PhoneNumber) && user.PhoneNumber != request.PhoneNumber)
                    throw new InvalidOperationException("Phone number already exists");
                user.PhoneNumber = request.PhoneNumber;
            }

            if (!string.IsNullOrEmpty(request.Address))
                user.Address = request.Address;

            if (request.DateOfBirth.HasValue)
                user.DateOfBirth = request.DateOfBirth.Value;

            if (request.Gender.HasValue)
                user.Gender = request.Gender.Value;

            user.UpdatedAt = DateTime.UtcNow;

            var updatedUser = await _repository.UpdateAsync(user);
            if (updatedUser == null)
                throw new InvalidOperationException("Failed to update user");

            return _mapper.Map<UserResponse>(updatedUser);
        }

        public async Task<BasicSuccessResponse> DeleteUserAsync(Guid userId)
        {
            var user = await _repository.FindAsync(userId);
            if (user == null || user.IsDeleted)
                throw new InvalidOperationException("User not found");

            var deletedUser = await _repository.DeleteAsync(user);
            if (deletedUser == null)
                throw new InvalidOperationException("Failed to delete user");

            return new BasicSuccessResponse
            {
                Success = true,
                Data = new { Message = "User deleted successfully" }
            };
        }
    }
}
