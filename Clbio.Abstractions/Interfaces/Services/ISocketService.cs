namespace Clbio.Abstractions.Interfaces.Services
{
    public interface ISocketService
    {
        // send notification to user
        Task SendToUserAsync(Guid userId, string method, object data, CancellationToken ct = default);
        // send update to all workspace members
        Task SendToWorkspaceAsync(Guid workspaceId, string method, object data, CancellationToken ct = default);
    }
}
