using Clbio.Application.DTOs.V1.WorkspaceMember;
using Clbio.Domain.Enums;
using Clbio.Shared.Results;

namespace Clbio.Application.Interfaces.EntityServices
{
    public interface IWorkspaceMemberAppService
    {
        Task<Result<List<ReadWorkspaceMemberDto>>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken ct = default);
        Task<Result<ReadWorkspaceMemberDto>> AddMemberAsync(Guid workspaceId, CreateWorkspaceMemberDto dto, CancellationToken ct = default);
        Task<Result<ReadWorkspaceMemberDto>> UpdateRoleAsync(Guid workspaceId, Guid targetUserId, WorkspaceRole newRole, Guid actorUserId, CancellationToken ct = default);
        Task<Result> RemoveMemberAsync(Guid workspaceId, Guid targetUserId, Guid actorUserId, CancellationToken ct = default);
        Task<Result> LeaveWorkspaceAsync(Guid workspaceId, Guid userId, CancellationToken ct = default);
    }
}
