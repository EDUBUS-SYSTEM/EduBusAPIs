using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Constants;

namespace APIs.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }
        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            var userRole = GetUserRole();
            
            if (userId != Guid.Empty)
            {
                // Add user to their personal group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
                
                // Add admin users to admin group
                if (userRole == Roles.Admin)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
                }
                
                _logger.LogInformation("User {UserId} with role {Role} connected to NotificationHub. ConnectionId: {ConnectionId}", 
                    userId, userRole, Context.ConnectionId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            var userRole = GetUserRole();
            
            if (userId != Guid.Empty)
            {
                // Remove user from their personal group
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
                
                // Remove admin users from admin group
                if (userRole == Roles.Admin)
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Admins");
                }
                
                _logger.LogInformation("User {UserId} with role {Role} disconnected from NotificationHub. ConnectionId: {ConnectionId}", 
                    userId, userRole, Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinGroup(string groupName)
        {
            var userId = GetUserId();
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            
            _logger.LogInformation("User {UserId} joined group {GroupName}", userId, groupName);
        }

        public async Task LeaveGroup(string groupName)
        {
            var userId = GetUserId();
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            
            _logger.LogInformation("User {UserId} left group {GroupName}", userId, groupName);
        }

        private Guid GetUserId()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            return Guid.Empty;
        }

        private string GetUserRole()
        {
            return Context.User?.FindFirst(ClaimTypes.Role)?.Value ?? Roles.Unknown;
        }
    }
}
