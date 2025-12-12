using Clbio.Abstractions.Interfaces.Services;
using Clbio.API.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Clbio.API.Services
{
    public class SignalRSocketService(IHubContext<AppHub> hubContext) : ISocketService
    {
        private readonly IHubContext<AppHub> _hubContext = hubContext;

        public async Task SendToUserAsync(Guid userId, string method, object data, CancellationToken ct = default)
        {
            // "send to group: user_{userId}"
            await _hubContext.Clients.Group($"user_{userId}").SendAsync(method, data, ct);
        }

        public async Task SendToWorkspaceAsync(Guid workspaceId, string method, object data, CancellationToken ct = default)
        {
            // "send to group: workspace_{workspaceId}"
            await _hubContext.Clients.Group($"workspace_{workspaceId}").SendAsync(method, data, ct);
        }
    }
}