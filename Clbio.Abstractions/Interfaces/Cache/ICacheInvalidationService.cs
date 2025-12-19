using Clbio.Domain.Enums;

namespace Clbio.Abstractions.Interfaces.Cache
{
    public interface ICacheInvalidationService
    {
        Task InvalidateUser(Guid userId);
        Task InvalidateWorkspace(Guid workspaceId);
        Task InvalidateMembership(Guid userId, Guid workspaceId);
        Task InvalidateWorkspaceRole(WorkspaceRole role);
        Task InvalidateUserInvitations(Guid userId);
    }
}
