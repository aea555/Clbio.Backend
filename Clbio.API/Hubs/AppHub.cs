using Clbio.Abstractions.Interfaces.Services;
using Clbio.API.Extensions;
using Clbio.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Clbio.API.Hubs
{
    [Authorize]
    public class AppHub(
        IUserPermissionService permissionService,
        IPresenceService presenceService,
        ILogger<AppHub> logger) : Hub
    {
        private readonly IUserPermissionService _permissionService = permissionService;
        private readonly IPresenceService _presenceService = presenceService;
        private readonly ILogger<AppHub> _logger = logger;

        public async Task JoinWorkspace(Guid workspaceId)
        {
            var userId = Context.User?.GetUserId();
            if (userId == null)
            {
                _logger.LogWarning("Anonymous connection attempted to join workspace {WorkspaceId}", workspaceId);
                throw new HubException("Unauthorized");
            }

            try
            {
                // 1. Does the user actually have permissions?
                var hasPermission = await _permissionService.HasPermissionAsync(
                    userId.Value,
                    Permission.ViewWorkspace,
                    workspaceId);

                if (!hasPermission.Value)
                {
                    _logger.LogWarning("Security Alert: User {UserId} tried to join Workspace {WorkspaceId} without permission.", userId, workspaceId);

                    throw new HubException("Access Denied: You do not have permission to view this workspace.");
                }

                // 2. Add to group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"workspace_{workspaceId}");

                _logger.LogInformation("User {UserId} joined workspace channel {WorkspaceId}", userId, workspaceId);
            }
            catch (Exception ex) when (ex is not HubException)
            {
                _logger.LogError(ex, "Error while joining workspace {WorkspaceId} for user {UserId}", workspaceId, userId);
                throw new HubException("An internal error occurred while joining the workspace channel.");
            }
        }

        public async Task LeaveWorkspace(Guid workspaceId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"workspace_{workspaceId}");

            _logger.LogDebug("User {UserId} left workspace channel {WorkspaceId}", Context.User?.GetUserId(), workspaceId);
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.GetUserId();
            if (userId != null)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

                // quick start, heartbeat at join.
                await _presenceService.HeartbeatAsync(userId.Value);
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // users will go offline on their own in 60s.
            await base.OnDisconnectedAsync(exception);
        }
    }
}