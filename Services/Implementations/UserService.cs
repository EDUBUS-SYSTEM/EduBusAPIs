using Data.Repos.Interfaces;
using Services.Contracts;
using Services.Models.UserAccount;
using Data.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using Utils;

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

        public async Task<UserListResponse> GetUsersAsync(string? status, string? search, int page, int perPage, string? sortBy, string? sortOrder, string? role)
        {
            // Build query
            var query = _repository.GetQueryable().Where(u => !u.IsDeleted);

			if (!string.IsNullOrEmpty(search))
			{
				query = query.Where(u =>
					u.FirstName.Contains(search) ||
					u.LastName.Contains(search) ||
					u.Email.Contains(search));
			}

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
				else if (status.ToLower() == "islocked")
				{
					query = query.Where(u => !u.IsDeleted && u.LockedUntil.HasValue && u.LockedUntil.Value > DateTime.UtcNow);
				}
				else if (status.ToLower() == "isnotlocked")
				{
					query = query.Where(u => !u.IsDeleted && (!u.LockedUntil.HasValue || u.LockedUntil.Value <= DateTime.UtcNow));
				}
			}

            // Apply role filter based on derived user type
            if (!string.IsNullOrWhiteSpace(role))
            {
                var normalizedRole = role.Trim().ToLowerInvariant();

                query = normalizedRole switch
                {
                    var r when r == "admin" => query.OfType<Admin>(),
                    var r when r == "driver" => query.OfType<Driver>(),
                    var r when r == "parent" => query.OfType<Parent>(),
                    var r when r == "supervisor" => query.OfType<Supervisor>(),
                    _ => query
                };
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

		public async Task<BasicSuccessResponse> LockUserAsync(Guid userId, DateTime? lockedUntil, string? reason, Guid lockedBy)
		{
			// Convert to UTC if the datetime is provided
			DateTime utcLockedUntil = lockedUntil.HasValue
            ? (lockedUntil.Value.Kind == DateTimeKind.Utc
	            ? lockedUntil.Value
	            : lockedUntil.Value.ToUniversalTime())
            : DateTime.UtcNow.AddYears(100);

			var affectedRows = await _repository.LockUserAsync(userId, utcLockedUntil, reason, lockedBy);

			if (affectedRows == 0)
				throw new InvalidOperationException("User not found or already locked");

			return new BasicSuccessResponse
			{
				Success = true,
				Data = new { Message = "User locked successfully", AffectedRows = affectedRows }
			};
		}

		public async Task<BasicSuccessResponse> UnlockUserAsync(Guid userId, Guid unlockedBy)
		{
			var affectedRows = await _repository.UnlockUserAsync(userId, unlockedBy);

			if (affectedRows == 0)
				throw new InvalidOperationException("User not found or already unlocked");

			return new BasicSuccessResponse
			{
				Success = true,
				Data = new { Message = "User unlocked successfully", AffectedRows = affectedRows }
			};
		}

		public async Task<BasicSuccessResponse> LockMultipleUsersAsync(List<Guid> userIds, DateTime? lockedUntil, string? reason, Guid lockedBy)
		{
			if (!userIds.Any())
				throw new InvalidOperationException("No user IDs provided");

			// Convert to UTC if the datetime is provided
			DateTime utcLockedUntil = lockedUntil.HasValue
		    ? (lockedUntil.Value.Kind == DateTimeKind.Utc
			    ? lockedUntil.Value
			    : lockedUntil.Value.ToUniversalTime())
		    : DateTime.UtcNow.AddYears(100);

			var affectedRows = await _repository.LockUsersAsync(userIds, utcLockedUntil, reason, lockedBy);

			if (affectedRows == 0)
				throw new InvalidOperationException("No users were found or updated");

			return new BasicSuccessResponse
			{
				Success = true,
				Data = new { Message = $"{affectedRows} users locked successfully", AffectedRows = affectedRows }
			};
		}

		public async Task<BasicSuccessResponse> UnlockMultipleUsersAsync(List<Guid> userIds, Guid unlockedBy)
		{
			if (!userIds.Any())
				throw new InvalidOperationException("No user IDs provided");

			var affectedRows = await _repository.UnlockUsersAsync(userIds, unlockedBy);

			if (affectedRows == 0)
				throw new InvalidOperationException("No users were found or updated");

			return new BasicSuccessResponse
			{
				Success = true,
				Data = new { Message = $"{affectedRows} users unlocked successfully", AffectedRows = affectedRows }
			};
		}

		public async Task<BasicSuccessResponse> ResetAllPasswordsAsync()
		{
			var hashedPassword = SecurityHelper.HashPassword("password");
			var affectedRows = await _repository.ResetAllPasswordsAsync(hashedPassword);

			if (affectedRows == 0)
				throw new InvalidOperationException("No users were found or updated");

			return new BasicSuccessResponse
			{
				Success = true,
				Data = new { Message = $"Password reset successfully to 'password' for {affectedRows} users", AffectedRows = affectedRows }
			};
		}

	}
}
