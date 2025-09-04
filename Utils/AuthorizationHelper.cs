using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Constants;

namespace Utils
{
    public static class AuthorizationHelper
    {
        /// <summary>
        /// Check if the current user can access/modify the specified user's data
        /// </summary>
        /// <param name="httpContext">Current HTTP context</param>
        /// <param name="targetUserId">Target user ID to check access for</param>
        /// <returns>True if user can access the data, false otherwise</returns>
        public static bool CanAccessUserData(HttpContext httpContext, Guid targetUserId)
        {
            // Get current user ID from JWT token
            var currentUserIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserIdClaim) || !Guid.TryParse(currentUserIdClaim, out var currentUserId))
            {
                return false;
            }

            // Admin can access any user's data
            if (httpContext.User.IsInRole(Roles.Admin))
            {
                return true;
            }

            // Regular users can only access their own data
            return currentUserId == targetUserId;
        }

        /// <summary>
        /// Check if the current user can access student data
        /// Admin can access any student, Parent can only access their own children
        /// </summary>
        /// <param name="httpContext">Current HTTP context</param>
        /// <param name="studentParentId">Parent ID of the student (can be null for students without parent)</param>
        /// <returns>True if user can access the student data, false otherwise</returns>
        public static bool CanAccessStudentData(HttpContext httpContext, Guid? studentParentId)
        {
            // Admin can access any student
            if (httpContext.User.IsInRole(Roles.Admin))
            {
                return true;
            }

            // If student has no parent, only admin can access
            if (!studentParentId.HasValue)
            {
                return false;
            }

            // Parent can only access their own children
            if (httpContext.User.IsInRole(Roles.Parent))
            {
                var currentUserIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserIdClaim) || !Guid.TryParse(currentUserIdClaim, out var currentUserId))
                {
                    return false;
                }

                // Parent's UserAccount ID should match the student's ParentId
                return currentUserId == studentParentId.Value;
            }

            return false;
        }

        /// <summary>
        /// Check if the current user can access parent data
        /// Admin can access any parent, Parent can only access their own data
        /// </summary>
        /// <param name="httpContext">Current HTTP context</param>
        /// <param name="parentId">Parent ID to check access for</param>
        /// <returns>True if user can access the parent data, false otherwise</returns>
        public static bool CanAccessParentData(HttpContext httpContext, Guid parentId)
        {
            // Admin can access any parent
            if (httpContext.User.IsInRole(Roles.Admin))
            {
                return true;
            }

            // Parent can only access their own data
            if (httpContext.User.IsInRole(Roles.Parent))
            {
                var currentUserIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserIdClaim) || !Guid.TryParse(currentUserIdClaim, out var currentUserId))
                {
                    return false;
                }

                return currentUserId == parentId;
            }

            return false;
        }

        /// <summary>
        /// Get current user ID from JWT token
        /// </summary>
        /// <param name="httpContext">Current HTTP context</param>
        /// <returns>Current user ID or null if not found</returns>
        public static Guid? GetCurrentUserId(HttpContext httpContext)
        {
            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return null;
            }
            return userId;
        }

        /// <summary>
        /// Check if current user has admin role
        /// </summary>
        /// <param name="httpContext">Current HTTP context</param>
        /// <returns>True if user is admin, false otherwise</returns>
        public static bool IsAdmin(HttpContext httpContext)
        {
            return httpContext.User.IsInRole(Roles.Admin);
        }

        /// <summary>
        /// Check if current user has specific role
        /// </summary>
        /// <param name="httpContext">Current HTTP context</param>
        /// <param name="role">Role to check</param>
        /// <returns>True if user has the role, false otherwise</returns>
        public static bool HasRole(HttpContext httpContext, string role)
        {
            return httpContext.User.IsInRole(role);
        }
    }
}
