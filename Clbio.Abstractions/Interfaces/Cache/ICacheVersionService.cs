using Clbio.Domain.Enums;

namespace Clbio.Abstractions.Interfaces.Cache
{
    public interface ICacheVersionService
    {
        Task<long> GetWorkspaceVersionAsync(Guid workspaceId);
        Task<long> BumpWorkspaceVersionAsync(Guid workspaceId);
        Task<long> GetWorkspaceRoleVersionAsync(WorkspaceRole role);
        Task<long> BumpWorkspaceRoleVersionAsync(WorkspaceRole role);
        Task<long> GetMembershipVersionAsync(Guid userId, Guid workspaceId);
        Task<long> IncrementMembershipVersionAsync(Guid userId, Guid workspaceId);
    }
}
